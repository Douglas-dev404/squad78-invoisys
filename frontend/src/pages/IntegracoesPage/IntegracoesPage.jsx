import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Layout } from '../../components/Layout/Layout';
import { Button } from '../../components/Button/Button';
import { InputField } from '../../components/InputField/InputField';
import { SelectField } from '../../components/SelectField/SelectField';
import { Icon } from '../../components/Icon/Icon';
import {
  obterIntegracoes,
  salvarIa,
  salvarJira,
  testarIa,
  testarJira,
} from '../../services/integracoes.service';
import {
  PROVEDORES_IA,
  dadosDoProvedor,
  errosPorCampo,
  formIaInicial,
  formJiraInicial,
  iaAlterada,
  jiraAlterado,
  payloadIa,
  payloadJira,
} from '../../utils/integracoes';

const CARD = 'rounded-xl border border-surface-variant/50 bg-surface-container-lowest p-md shadow-[0px_4px_12px_rgba(27,67,50,0.08)] md:p-lg';

function Selo({ configurado }) {
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full px-2 py-1 text-label-sm font-semibold ${
        configurado ? 'bg-green-100 text-green-800' : 'bg-amber-100 text-amber-800'
      }`}
    >
      <Icon name={configurado ? 'check_circle' : 'warning'} size={16} />
      {configurado ? 'Configurado' : 'Não configurado'}
    </span>
  );
}

function ResultadoTeste({ resultado }) {
  if (!resultado) return null;
  return (
    <p
      role="status"
      className={`rounded-lg border p-sm font-body-sm text-body-sm ${
        resultado.ok
          ? 'border-green-200 bg-green-50 text-green-900'
          : 'border-error/20 bg-error-container text-on-error-container'
      }`}
    >
      {resultado.ok ? 'Conexão ok. ' : 'Falhou: '}
      {resultado.mensagem}
      {resultado.duracaoMs != null && ` (${resultado.duracaoMs} ms)`}
    </p>
  );
}

function Alerta({ mensagem }) {
  if (!mensagem) return null;
  return (
    <p role="alert" className="rounded-lg border border-error/20 bg-error-container p-sm font-body-sm text-body-sm text-on-error-container">
      {mensagem}
    </p>
  );
}

function CartaoJira({ jira, onSalvo }) {
  const [form, setForm] = useState(() => formJiraInicial(jira));
  const [erros, setErros] = useState({});
  const [erro, setErro] = useState(null);
  const [salvando, setSalvando] = useState(false);
  const [testando, setTestando] = useState(false);
  const [resultado, setResultado] = useState(null);
  const [salvo, setSalvo] = useState(false);

  const alterar = (campo) => (event) => {
    setForm((atual) => ({ ...atual, [campo]: event.target.value }));
    setErros((atuais) => ({ ...atuais, [campo]: undefined }));
    setSalvo(false);
    setResultado(null);
  };

  const submeter = async (event) => {
    event.preventDefault();
    setSalvando(true);
    setErro(null);
    try {
      const novo = await salvarJira(payloadJira(form));
      setForm(formJiraInicial(novo.jira));
      setSalvo(true);
      onSalvo(novo);
    } catch (error) {
      if (error?.status === 422 && Object.keys(error.errors ?? {}).length > 0) setErros(errosPorCampo(error.errors));
      else setErro(error?.detail || error?.message || 'Não foi possível salvar o Jira.');
    } finally {
      setSalvando(false);
    }
  };

  const testar = async () => {
    setTestando(true);
    setResultado(null);
    try {
      setResultado(await testarJira());
    } catch (error) {
      setResultado({ ok: false, mensagem: error?.detail || error?.message || 'Não foi possível testar.' });
    } finally {
      setTestando(false);
    }
  };

  const alterado = jiraAlterado(form, jira);

  return (
    <section className={`${CARD} space-y-md`} aria-labelledby="titulo-jira">
      <header className="flex flex-wrap items-center justify-between gap-sm">
        <h2 id="titulo-jira" className="font-headline-md text-headline-md text-primary">Jira</h2>
        <Selo configurado={jira.configurado} />
      </header>
      <p className="font-body-sm text-body-sm text-on-surface-variant">
        De onde o InvoiSys lê as versões e as histórias. O token é gerado em id.atlassian.com e vale só para a conta do e-mail.
      </p>
      <form className="space-y-md" onSubmit={submeter} noValidate>
        <div className="grid grid-cols-1 gap-gutter md:grid-cols-2">
          <InputField id="jira-url" name="baseUrl" label="URL do Jira" value={form.baseUrl} onChange={alterar('baseUrl')}
            placeholder="https://suaempresa.atlassian.net" error={erros.baseUrl} disabled={salvando} />
          <InputField id="jira-projeto" name="projeto" label="Chave do projeto" value={form.projeto} onChange={alterar('projeto')}
            placeholder="INV" error={erros.projeto} disabled={salvando} />
          <InputField id="jira-email" name="email" type="email" label="E-mail da conta" value={form.email} onChange={alterar('email')}
            placeholder="voce@empresa.com" error={erros.email} disabled={salvando} autoComplete="off" />
          <InputField id="jira-token" name="apiToken" type="password" label="API token" value={form.apiToken} onChange={alterar('apiToken')}
            placeholder={jira.tokenConfigurado ? '•••••••• (deixe em branco para manter)' : 'Cole o token do Jira'}
            error={erros.apiToken} disabled={salvando} autoComplete="new-password" />
        </div>
        <Alerta mensagem={erro} />
        <ResultadoTeste resultado={resultado} />
        {salvo && !alterado && <p role="status" className="font-body-sm text-body-sm text-green-800">Jira salvo.</p>}
        <div className="flex flex-col-reverse justify-end gap-sm sm:flex-row">
          <Button onClick={testar} isLoading={testando} disabled={alterado || salvando || !jira.configurado}
            className="!bg-transparent !text-primary border border-primary !shadow-none hover:!bg-primary/5">
            Testar conexão
          </Button>
          <Button type="submit" isLoading={salvando} disabled={!alterado}>Salvar Jira</Button>
        </div>
        {alterado && jira.configurado && (
          <p className="text-right font-label-sm text-label-sm text-on-surface-variant">Salve para poder testar com os novos valores.</p>
        )}
      </form>
    </section>
  );
}

function CartaoIa({ ia, onSalvo }) {
  const [form, setForm] = useState(() => formIaInicial(ia));
  const [erros, setErros] = useState({});
  const [erro, setErro] = useState(null);
  const [salvando, setSalvando] = useState(false);
  const [testando, setTestando] = useState(false);
  const [resultado, setResultado] = useState(null);
  const [salvo, setSalvo] = useState(false);

  const dadosEscolhido = dadosDoProvedor(ia, form.provedor);

  const mudar = () => {
    setSalvo(false);
    setResultado(null);
  };

  const trocarProvedor = (event) => {
    const provedor = event.target.value;
    setForm({ provedor, modelo: dadosDoProvedor(ia, provedor)?.modelo ?? '', apiKey: '' });
    setErros({});
    mudar();
  };

  const alterar = (campo) => (event) => {
    setForm((atual) => ({ ...atual, [campo]: event.target.value }));
    setErros((atuais) => ({ ...atuais, [campo]: undefined }));
    mudar();
  };

  const submeter = async (event) => {
    event.preventDefault();
    setSalvando(true);
    setErro(null);
    try {
      const novo = await salvarIa(payloadIa(form));
      setForm(formIaInicial(novo.ia));
      setSalvo(true);
      onSalvo(novo);
    } catch (error) {
      if (error?.status === 422 && Object.keys(error.errors ?? {}).length > 0) setErros(errosPorCampo(error.errors));
      else setErro(error?.detail || error?.message || 'Não foi possível salvar a IA.');
    } finally {
      setSalvando(false);
    }
  };

  const testar = async () => {
    setTestando(true);
    setResultado(null);
    try {
      setResultado(await testarIa());
    } catch (error) {
      setResultado({ ok: false, mensagem: error?.detail || error?.message || 'Não foi possível testar.' });
    } finally {
      setTestando(false);
    }
  };

  const alterada = iaAlterada(form, ia);

  return (
    <section className={`${CARD} space-y-md`} aria-labelledby="titulo-ia">
      <header className="flex flex-wrap items-center justify-between gap-sm">
        <h2 id="titulo-ia" className="font-headline-md text-headline-md text-primary">Inteligência Artificial</h2>
        <Selo configurado={ia.configurado} />
      </header>
      <p className="font-body-sm text-body-sm text-on-surface-variant">
        O modelo que agrupa as histórias e escreve o comunicado. A troca vale na próxima execução, sem reiniciar a API.{' '}
        <Link to="/execucao-ia" className="text-primary underline">Acompanhar a execução ao vivo</Link>
      </p>
      <form className="space-y-md" onSubmit={submeter} noValidate>
        <div className="grid grid-cols-1 gap-gutter md:grid-cols-2">
          <SelectField id="ia-provedor" name="provedor" label="Provedor" value={form.provedor} onChange={trocarProvedor}
            options={PROVEDORES_IA} error={erros.provedor} disabled={salvando} />
          <InputField id="ia-modelo" name="modelo" label="Modelo" value={form.modelo} onChange={alterar('modelo')}
            placeholder={form.provedor === 'OpenAi' ? 'gpt-4o-mini' : 'openai/gpt-4o-mini'} error={erros.modelo} disabled={salvando} />
          <div className="md:col-span-2">
            <InputField id="ia-chave" name="apiKey" type="password" label="Chave de API" value={form.apiKey} onChange={alterar('apiKey')}
              placeholder={dadosEscolhido?.chaveConfigurada ? '•••••••• (deixe em branco para manter)' : 'Cole a chave do provedor'}
              error={erros.apiKey} disabled={salvando} autoComplete="new-password" />
          </div>
        </div>
        <Alerta mensagem={erro} />
        <ResultadoTeste resultado={resultado} />
        {salvo && !alterada && <p role="status" className="font-body-sm text-body-sm text-green-800">IA salva.</p>}
        <div className="flex flex-col-reverse justify-end gap-sm sm:flex-row">
          <Button onClick={testar} isLoading={testando} disabled={alterada || salvando || !ia.configurado}
            className="!bg-transparent !text-primary border border-primary !shadow-none hover:!bg-primary/5">
            Testar conexão
          </Button>
          <Button type="submit" isLoading={salvando} disabled={!alterada}>Salvar IA</Button>
        </div>
        {alterada && ia.configurado && (
          <p className="text-right font-label-sm text-label-sm text-on-surface-variant">Salve para poder testar com os novos valores.</p>
        )}
      </form>
    </section>
  );
}

export function IntegracoesPage() {
  const [tentativa, setTentativa] = useState(0);
  const [resultado, setResultado] = useState({ tentativa: -1, dados: null, erro: null });

  useEffect(() => {
    const controller = new AbortController();
    obterIntegracoes({ signal: controller.signal })
      .then((dados) => setResultado({ tentativa, dados, erro: null }))
      .catch((error) => {
        if (controller.signal.aborted) return;
        setResultado({ tentativa, dados: null, erro: error?.detail || error?.message || 'Não foi possível carregar as integrações.' });
      });
    return () => controller.abort();
  }, [tentativa]);

  const atual = resultado.tentativa === tentativa;
  const dados = atual ? resultado.dados : null;
  const erro = atual ? resultado.erro : null;
  const tentarDeNovo = () => setTentativa((n) => n + 1);
  // depois de salvar, a visão devolvida pela API vira a fonte da verdade sem novo GET
  const aoSalvar = (novo) => setResultado({ tentativa, dados: novo, erro: null });

  return (
    <Layout>
      <div className="mx-auto w-full max-w-4xl space-y-lg">
        <div>
          <p className="font-label-md text-label-md text-on-surface-variant">Configurações</p>
          <h1 className="font-headline-lg text-headline-lg text-primary">Integrações</h1>
          <p className="font-body-md text-body-md text-on-surface-variant">
            Conecte o InvoiSys ao Jira e à IA. Os segredos ficam cifrados e nunca voltam para a tela.
          </p>
        </div>
        {erro && (
          <div role="alert" className="flex flex-col items-start gap-sm rounded-xl border border-error bg-error-container p-md sm:flex-row sm:items-center sm:justify-between">
            <p className="font-body-md text-body-md text-on-error-container">{erro}</p>
            <Button onClick={tentarDeNovo}>Tentar de novo</Button>
          </div>
        )}
        {!dados && !erro && (
          <div role="status" aria-live="polite" className="space-y-md">
            <div className="h-64 animate-pulse rounded-xl bg-surface-container" />
            <div className="h-64 animate-pulse rounded-xl bg-surface-container" />
            <span className="sr-only">Carregando integrações…</span>
          </div>
        )}
        {dados && (
          <>
            <CartaoJira jira={dados.jira} onSalvo={aoSalvar} />
            <CartaoIa ia={dados.ia} onSalvo={aoSalvar} />
          </>
        )}
      </div>
    </Layout>
  );
}
