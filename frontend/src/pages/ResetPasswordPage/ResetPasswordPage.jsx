import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Alert } from '../../components/Alert/Alert';
import { BrandLogoCard } from '../../components/BrandLogoCard/BrandLogoCard';
import { Button } from '../../components/Button/Button';
import { InputField } from '../../components/InputField/InputField';
import { resetPassword } from '../../services/auth.service';
import { BRAND_LOGO_URL, UI_STRINGS } from '../../utils/constants';

export function ResetPasswordPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [token] = useState(() => searchParams.get('token'));
  const [senha, setSenha] = useState('');
  const [confirmacao, setConfirmacao] = useState('');
  const [mostrarSenha, setMostrarSenha] = useState(false);
  const [erros, setErros] = useState({});
  const [erro, setErro] = useState(null);
  const [enviando, setEnviando] = useState(false);
  const [sucesso, setSucesso] = useState(false);

  useEffect(() => {
    if (token) navigate('/redefinir-senha', { replace: true });
  }, [navigate, token]);

  useEffect(() => {
    if (!sucesso) return undefined;
    const timeout = window.setTimeout(() => navigate('/login', { replace: true }), 3000);
    return () => window.clearTimeout(timeout);
  }, [navigate, sucesso]);

  const validar = () => {
    const proximosErros = {};
    if (senha.length < 8) proximosErros.novaSenha = 'A senha deve ter pelo menos 8 caracteres.';
    if (confirmacao !== senha) proximosErros.confirmacao = 'As senhas precisam ser iguais.';
    setErros(proximosErros);
    return Object.keys(proximosErros).length === 0;
  };

  const enviar = async (event) => {
    event.preventDefault();
    if (!validar()) return;
    setEnviando(true);
    setErro(null);
    try {
      await resetPassword({ token, novaSenha: senha });
      setSucesso(true);
    } catch (error) {
      setErros(error?.errors ?? {});
      setErro(error?.detail ?? error?.message ?? 'Não foi possível alterar a senha.');
    } finally {
      setEnviando(false);
    }
  };

  if (!token) {
    return <main className="bg-pattern min-h-screen flex items-center justify-center p-gutter"><section className="w-full max-w-md bg-surface-container-lowest rounded-xl border border-outline-variant/30 p-lg text-center"><BrandLogoCard logoUrl={BRAND_LOGO_URL} /><h1 className="mt-md text-headline-lg text-primary">Link inválido</h1><p className="mt-sm text-on-surface-variant">Peça um novo link de recuperação de senha.</p><Button className="mt-lg" onClick={() => navigate('/esqueci-senha')}>Pedir novo link</Button></section></main>;
  }

  return <main className="bg-pattern min-h-screen flex items-center justify-center p-gutter"><section className="w-full max-w-md bg-surface-container-lowest rounded-xl border border-outline-variant/30 p-lg"><header className="text-center mb-lg"><BrandLogoCard logoUrl={BRAND_LOGO_URL} /><h1 className="mt-md text-headline-lg text-primary">Redefinir senha</h1><p className="mt-xs text-on-surface-variant">Defina uma nova senha para acessar sua conta.</p></header>{sucesso ? <><Alert type="success" message="Senha alterada com sucesso." /><Button fullWidth onClick={() => navigate('/login')}>Ir para o login</Button></> : <form onSubmit={enviar} noValidate className="space-y-md">{erro && <Alert type="error" message={erro} onClose={() => setErro(null)} />}<InputField id="novaSenha" name="novaSenha" label="Nova senha" type={mostrarSenha ? 'text' : 'password'} value={senha} onChange={(event) => setSenha(event.target.value)} error={erros.novaSenha} required autoComplete="new-password" /><InputField id="confirmacao" name="confirmacao" label="Confirmar nova senha" type={mostrarSenha ? 'text' : 'password'} value={confirmacao} onChange={(event) => setConfirmacao(event.target.value)} error={erros.confirmacao} required autoComplete="new-password" /><label className="flex items-center gap-sm text-body-sm"><input type="checkbox" checked={mostrarSenha} onChange={(event) => setMostrarSenha(event.target.checked)} /> Mostrar senha</label><Button type="submit" fullWidth isLoading={enviando}>Alterar senha</Button><Button type="button" fullWidth className="bg-surface-container text-on-surface" onClick={() => navigate('/esqueci-senha')}>Pedir novo link</Button></form>}</section><footer className="absolute bottom-md text-center text-body-sm text-on-surface-variant">{UI_STRINGS.COPYRIGHT_TEXT}</footer></main>;
}
