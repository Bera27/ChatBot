'use strict';

/*
  Painel do atendimento.

  Como esta página conversa com o sistema:
  o navegador baixa estes arquivos e o JavaScript chama a API com fetch(),
  mandando a chave no cabeçalho X-Api-Key. A API devolve JSON e a página
  desenha esse JSON na tela. A página não guarda nada além da chave, e só
  enquanto a aba estiver aberta (sessionStorage).

  Regra de segurança: todo texto que vem do servidor (inclusive as mensagens
  dos clientes) entra na página com textContent, nunca com innerHTML.
  Assim, alguém que mande "<script>" por WhatsApp não executa nada aqui.
*/

const CHAVE_SALVA = 'atendimento_chave';
const INTERVALO_ATUALIZAR_MS = 30000;
const TAMANHO_MINIMO_PALAVRA = 3;

const $ = (seletor) => document.querySelector(seletor);

let chaveDeAcesso = null;
let abaAtual = 'fila';
let respostas = [];       // gatilhos carregados da API
let editandoId = null;    // null = criando uma resposta nova
let temporizadorAviso = null;

class ErroDeAcesso extends Error {}

/* ---------- utilidades ---------- */

// Cria um elemento: h('p', { class: 'x' }, 'texto', outroElemento)
function h(tag, atributos = {}, ...filhos) {
  const el = document.createElement(tag);
  for (const [nome, valor] of Object.entries(atributos)) {
    if (valor === false || valor == null) continue;
    el.setAttribute(nome, valor === true ? '' : valor);
  }
  for (const filho of filhos) {
    el.append(filho);   // texto vira nó de texto, nunca HTML
  }
  return el;
}

function lerChaveSalva() {
  try { return sessionStorage.getItem(CHAVE_SALVA); } catch { return null; }
}

function salvarChave(valor) {
  try {
    if (valor) sessionStorage.setItem(CHAVE_SALVA, valor);
    else sessionStorage.removeItem(CHAVE_SALVA);
  } catch { /* navegador sem armazenamento: a chave só vale nesta tela */ }
}

// "whatsapp:+5515996783034" -> "(15) 99678-3034"
function formatarTelefone(valor) {
  const digitos = valor.replace(/\D/g, '');
  if (digitos.startsWith('55') && digitos.length === 13)
    return `(${digitos.slice(2, 4)}) ${digitos.slice(4, 9)}-${digitos.slice(9)}`;
  if (digitos.startsWith('55') && digitos.length === 12)
    return `(${digitos.slice(2, 4)}) ${digitos.slice(4, 8)}-${digitos.slice(8)}`;
  return digitos ? `+${digitos}` : valor;
}

// A API guarda a hora em UTC, mas manda sem o "Z" no fim. Sem ele o navegador
// entenderia como hora local e mostraria 3 horas a mais. Aqui acrescentamos o Z.
function lerDataUtc(texto) {
  let t = texto.replace(/(\.\d{3})\d+/, '$1');   // alguns navegadores não aceitam mais de 3 casas
  if (!/(Z|[+-]\d{2}:\d{2})$/i.test(t)) t += 'Z';
  return new Date(t);
}

function minutosDesde(data) {
  return Math.max(0, Math.floor((Date.now() - data.getTime()) / 60000));
}

function descreverEspera(minutos, data) {
  if (minutos < 1) return 'agora';
  if (minutos < 60) return `há ${minutos} min`;
  const horas = Math.floor(minutos / 60);
  if (horas < 24) return `há ${horas} h`;
  return data.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }) +
         ' às ' + data.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
}

function mostrarAviso(texto, comErro = false) {
  const aviso = $('#aviso');
  aviso.textContent = texto;
  aviso.classList.toggle('com-erro', comErro);
  aviso.classList.add('visivel');
  clearTimeout(temporizadorAviso);
  temporizadorAviso = setTimeout(() => aviso.classList.remove('visivel'), comErro ? 6000 : 3500);
}

/* ---------- comunicação com a API ---------- */

function mensagemDeErro(corpo, status) {
  try {
    const dados = JSON.parse(corpo);
    if (dados && dados.errors) {
      const primeira = Object.values(dados.errors).flat()[0];
      if (primeira) return String(primeira);
    }
    if (dados && dados.title) return String(dados.title);
  } catch { /* não era JSON: a API mandou texto simples */ }
  return corpo || `Algo deu errado (erro ${status}).`;
}

async function api(caminho, { metodo = 'GET', corpo } = {}) {
  const cabecalhos = { 'X-Api-Key': chaveDeAcesso || '' };
  if (corpo !== undefined) cabecalhos['Content-Type'] = 'application/json';

  let resposta;
  try {
    resposta = await fetch(caminho, {
      method: metodo,
      headers: cabecalhos,
      body: corpo === undefined ? undefined : JSON.stringify(corpo),
    });
  } catch {
    throw new Error('Não foi possível falar com o servidor. Confira a conexão e tente de novo.');
  }

  if (resposta.status === 401) throw new ErroDeAcesso();
  if (resposta.status === 204) return null;

  const texto = await resposta.text();
  if (!resposta.ok) throw new Error(mensagemDeErro(texto, resposta.status));
  return texto ? JSON.parse(texto) : null;
}

/* ---------- entrada e saída ---------- */

function mostrarEntrada(mensagem) {
  chaveDeAcesso = null;
  salvarChave(null);
  $('#topo').hidden = true;
  $('#fila').hidden = true;
  $('#respostas').hidden = true;
  $('#entrada').hidden = false;
  $('#campo-chave').value = '';
  const erro = $('#erro-entrada');
  erro.textContent = mensagem || '';
  erro.hidden = !mensagem;
  $('#campo-chave').focus();
}

function mostrarPainel(carregar = true) {
  $('#entrada').hidden = true;
  $('#topo').hidden = false;
  selecionarAba(abaAtual, carregar);
}

function tratarErro(erro) {
  if (erro instanceof ErroDeAcesso) {
    mostrarEntrada('Sua sessão expirou ou a chave mudou. Entre de novo.');
    return;
  }
  mostrarAviso(erro.message, true);
}

async function entrar(evento) {
  evento.preventDefault();
  const digitada = $('#campo-chave').value.trim();
  if (!digitada) return;

  const botao = evento.submitter;
  botao.disabled = true;
  chaveDeAcesso = digitada;
  try {
    // Pedir a fila já serve para conferir a chave.
    const fila = await api('/api/historico/pendentes');
    salvarChave(digitada);
    mostrarPainel(false);   // a fila já veio na chamada acima
    desenharFila(fila);
  } catch (erro) {
    if (erro instanceof ErroDeAcesso) {
      mostrarEntrada('Chave incorreta. Confira com quem instalou o sistema.');
    } else {
      chaveDeAcesso = null;
      const e = $('#erro-entrada');
      e.textContent = erro.message;
      e.hidden = false;
    }
  } finally {
    botao.disabled = false;
  }
}

function sair() {
  mostrarEntrada('');
}

/* ---------- abas ---------- */

function selecionarAba(nome, carregar = true) {
  abaAtual = nome;
  for (const botao of document.querySelectorAll('.aba')) {
    if (botao.dataset.aba === nome) botao.setAttribute('aria-current', 'page');
    else botao.removeAttribute('aria-current');
  }
  $('#fila').hidden = nome !== 'fila';
  $('#respostas').hidden = nome !== 'respostas';

  if (!carregar) return;
  if (nome === 'fila') carregarFila().catch(tratarErro);
  else carregarRespostas().catch(tratarErro);
}

/* ---------- fila de atendimento ---------- */

async function carregarFila() {
  desenharFila(await api('/api/historico/pendentes'));
}

function atualizarContagem(total) {
  const marca = $('#contagem');
  marca.textContent = String(total);
  marca.hidden = total === 0;
}

function desenharFila(lista) {
  atualizarContagem(lista.length);
  const area = $('#lista-fila');
  area.replaceChildren();

  if (lista.length === 0) {
    area.append(h('p', { class: 'vazio' },
      h('strong', {}, 'Ninguém esperando.'),
      'Quando o bot não entender uma mensagem, ela aparece aqui.'));
    return;
  }

  for (const item of lista) area.append(criarItemDaFila(item));
}

function criarItemDaFila(item) {
  const data = lerDataUtc(item.criadoEm);
  const minutos = minutosDesde(data);
  const classeEspera = minutos >= 120 ? 'atrasada' : minutos >= 30 ? 'longa' : '';

  const digitos = item.numeroWhatsApp.replace(/\D/g, '');
  const completa = data.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });

  const botaoAtender = h('button', { class: 'botao', type: 'button' }, 'Marcar como atendido');
  botaoAtender.addEventListener('click', () => marcarComoAtendido(item.id, botaoAtender));

  const acoes = h('div', { class: 'acoes' });
  if (digitos) {
    acoes.append(h('a', {
      class: 'botao-sec',
      href: `https://wa.me/${digitos}`,
      target: '_blank',
      rel: 'noopener noreferrer',
    }, 'Abrir conversa'));
  }
  acoes.append(botaoAtender);

  return h('article', { class: 'item-fila' },
    h('div', { class: 'item-fila-topo' },
      h('span', { class: 'telefone' }, formatarTelefone(item.numeroWhatsApp)),
      h('time', { class: `espera ${classeEspera}`.trim(), datetime: data.toISOString(), title: completa },
        descreverEspera(minutos, data))),
    h('p', { class: 'balao' }, item.texto),
    acoes);
}

async function marcarComoAtendido(id, botao) {
  botao.disabled = true;
  try {
    await api(`/api/historico/${id}/atender`, { metodo: 'PATCH' });
    mostrarAviso('Marcado como atendido. O bot volta a responder esse cliente.');
    await carregarFila();
  } catch (erro) {
    botao.disabled = false;
    tratarErro(erro);
  }
}

/* ---------- respostas automáticas ---------- */

async function carregarRespostas() {
  respostas = await api('/api/gatilhos');
  desenharRespostas();
}

function desenharRespostas() {
  const area = $('#lista-respostas');
  area.replaceChildren();

  if (respostas.length === 0) {
    area.append(h('p', { class: 'vazio' },
      h('strong', {}, 'Nenhuma resposta cadastrada.'),
      'Sem respostas, o bot chama um atendente em todas as mensagens. Use "Nova resposta" para começar.'));
    return;
  }

  for (const gatilho of respostas) area.append(criarItemDeResposta(gatilho));
}

function criarItemDeResposta(gatilho) {
  const editar = h('button', { class: 'botao-texto', type: 'button' }, 'Editar');
  editar.addEventListener('click', () => abrirDialogo(gatilho));

  const excluir = h('button', { class: 'botao-texto perigo', type: 'button' }, 'Excluir');
  excluir.addEventListener('click', () => excluirResposta(gatilho, excluir));

  return h('article', { class: 'item-resposta' },
    h('div', { class: 'item-resposta-topo' },
      h('h3', {}, gatilho.nome),
      h('div', {}, editar, excluir)),
    h('p', { class: 'legenda' }, 'Quando o cliente escrever'),
    h('div', { class: 'palavras' }, ...gatilho.chaves.map((c) => h('span', { class: 'palavra' }, c))),
    h('p', { class: 'legenda direita' }, 'O bot responde'),
    h('p', { class: 'balao-bot' }, gatilho.resposta));
}

async function excluirResposta(gatilho, botao) {
  if (!confirm(`Excluir a resposta "${gatilho.nome}"? Essa ação não pode ser desfeita.`)) return;
  botao.disabled = true;
  try {
    await api(`/api/gatilhos/${gatilho.id}`, { metodo: 'DELETE' });
    mostrarAviso('Excluído.');
    await carregarRespostas();
  } catch (erro) {
    botao.disabled = false;
    tratarErro(erro);
  }
}

/* ---------- criar e editar ---------- */

function abrirDialogo(gatilho) {
  editandoId = gatilho ? gatilho.id : null;
  $('#dialogo-titulo').textContent = gatilho ? 'Editar resposta' : 'Nova resposta';
  $('#campo-nome').value = gatilho ? gatilho.nome : '';
  $('#campo-chaves').value = gatilho ? gatilho.chaves.join(', ') : '';
  $('#campo-resposta').value = gatilho ? gatilho.resposta : '';
  mostrarErroDoDialogo('');
  $('#dialogo').showModal();
  $('#campo-nome').focus();
}

function mostrarErroDoDialogo(mensagem) {
  const erro = $('#erro-dialogo');
  erro.textContent = mensagem;
  erro.hidden = !mensagem;
}

// "preco, Valor\nvalores" -> ["preco", "Valor", "valores"] (sem repetidas)
function lerPalavras(texto) {
  const vistas = new Set();
  const palavras = [];
  for (const parte of texto.split(/[,\n]/)) {
    const palavra = parte.trim();
    const chave = palavra.toLowerCase();
    if (palavra && !vistas.has(chave)) {
      vistas.add(chave);
      palavras.push(palavra);
    }
  }
  return palavras;
}

function validarFormulario(nome, palavras, resposta) {
  if (!nome) return 'Dê um nome para esta resposta.';
  if (palavras.length === 0) return 'Informe pelo menos uma palavra que ativa a resposta.';
  const curta = palavras.find((p) => p.length < TAMANHO_MINIMO_PALAVRA);
  if (curta) {
    return `A palavra "${curta}" é muito curta e ativaria quase toda mensagem. ` +
           `Use palavras com pelo menos ${TAMANHO_MINIMO_PALAVRA} letras.`;
  }
  if (!resposta) return 'Escreva o que o bot deve responder.';
  return '';
}

async function salvarResposta(evento) {
  evento.preventDefault();
  const nome = $('#campo-nome').value.trim();
  const palavras = lerPalavras($('#campo-chaves').value);
  const resposta = $('#campo-resposta').value.trim();

  const problema = validarFormulario(nome, palavras, resposta);
  if (problema) {
    mostrarErroDoDialogo(problema);
    return;
  }

  const botao = evento.submitter;
  botao.disabled = true;
  try {
    const corpo = { nome, chaves: palavras, resposta };
    if (editandoId === null) await api('/api/gatilhos', { metodo: 'POST', corpo });
    else await api(`/api/gatilhos/${editandoId}`, { metodo: 'PUT', corpo });

    $('#dialogo').close();
    mostrarAviso('Salvo.');
    await carregarRespostas();
  } catch (erro) {
    if (erro instanceof ErroDeAcesso) {
      $('#dialogo').close();
      tratarErro(erro);
    } else {
      mostrarErroDoDialogo(erro.message);
    }
  } finally {
    botao.disabled = false;
  }
}

/* ---------- início ---------- */

function iniciar() {
  $('#form-entrada').addEventListener('submit', entrar);
  $('#botao-sair').addEventListener('click', sair);
  $('#atualizar').addEventListener('click', () => carregarFila().catch(tratarErro));
  $('#nova').addEventListener('click', () => abrirDialogo(null));
  $('#cancelar').addEventListener('click', () => $('#dialogo').close());
  $('#form-resposta').addEventListener('submit', salvarResposta);

  for (const botao of document.querySelectorAll('.aba')) {
    botao.addEventListener('click', () => selecionarAba(botao.dataset.aba));
  }

  // A fila se atualiza sozinha enquanto a aba está visível.
  setInterval(() => {
    if (chaveDeAcesso && abaAtual === 'fila' && !document.hidden) {
      carregarFila().catch(tratarErro);
    }
  }, INTERVALO_ATUALIZAR_MS);

  chaveDeAcesso = lerChaveSalva();
  if (chaveDeAcesso) {
    mostrarPainel();
  } else {
    mostrarEntrada('');
  }
}

document.addEventListener('DOMContentLoaded', iniciar);
