import { useState } from 'react';
import { Alert } from '../Alert/Alert';
import { Button } from '../Button/Button';
import { BOTAO_SECUNDARIO, Dialogo } from '../Dialogo/Dialogo';
import { InputField } from '../InputField/InputField';
import { SelectField } from '../SelectField/SelectField';
import { validarDestinatario } from '../../utils/destinatarios';
import { erroDoCampo, mensagemDeErro, PUBLICO_LABEL } from '../../utils/revisao';

const OPCOES_PUBLICO = Object.entries(PUBLICO_LABEL).map(([value, label]) => ({ value, label }));

/**
 * Cadastro e edição de um destinatário (#147). 409 (e-mail repetido no público) e `errors.<campo>` do 422
 * aparecem embaixo do campo; outros erros, no topo.
 * @param {Object} props
 * @param {{ id: string, nome: string, email: string, publico: string }|null} props.destinatario null = novo
 * @param {string} [props.publicoInicial] público já escolhido ao abrir um novo (vem do filtro da tela)
 * @param {(id: string|null, dados: { nome, email, publico }) => Promise<void>} props.onSalvar
 * @param {Function} props.onClose
 */
export function DestinatarioModal({ destinatario, publicoInicial = '', onSalvar, onClose }) {
  const [valores, setValores] = useState({
    nome: destinatario?.nome ?? '',
    email: destinatario?.email ?? '',
    publico: destinatario?.publico ?? publicoInicial,
  });
  const [erros, setErros] = useState({});
  const [erroGeral, setErroGeral] = useState(null);
  const [salvando, setSalvando] = useState(false);

  const mudar = (campo) => (event) => {
    setValores((atual) => ({ ...atual, [campo]: event.target.value }));
    setErros((atual) => ({ ...atual, [campo]: undefined }));
  };

  const enviar = async (event) => {
    event.preventDefault();
    setErroGeral(null);
    const { valido, erros: errosLocais } = validarDestinatario(valores);
    if (!valido) {
      setErros(errosLocais);
      return;
    }

    setSalvando(true);
    try {
      await onSalvar(destinatario?.id ?? null, valores);
      onClose();
    } catch (err) {
      if (err?.status === 409) {
        setErros({ email: mensagemDeErro(err) });
      } else if (err?.status === 422 && err.errors && Object.keys(err.errors).length > 0) {
        setErros({
          nome: erroDoCampo(err, 'nome') ?? undefined,
          email: erroDoCampo(err, 'email') ?? undefined,
          publico: erroDoCampo(err, 'publico') ?? undefined,
        });
      } else {
        setErroGeral(mensagemDeErro(err, 'Não foi possível salvar o destinatário.'));
      }
    } finally {
      setSalvando(false);
    }
  };

  return (
    <Dialogo titulo={destinatario ? 'Editar destinatário' : 'Novo destinatário'} onClose={onClose} bloqueado={salvando}>
      <form noValidate onSubmit={enviar} className="space-y-md">
        <Alert type="error" message={erroGeral} />
        <InputField
          id="destinatario-nome"
          name="nome"
          label="Nome"
          value={valores.nome}
          onChange={mudar('nome')}
          placeholder="Ex.: Financeiro ACME"
          required
          error={erros.nome}
          disabled={salvando}
        />
        <InputField
          id="destinatario-email"
          name="email"
          type="email"
          label="E-mail"
          value={valores.email}
          onChange={mudar('email')}
          placeholder="nome@empresa.com.br"
          autoComplete="off"
          required
          error={erros.email}
          disabled={salvando}
        />
        <SelectField
          id="destinatario-publico"
          name="publico"
          label="Público"
          value={valores.publico}
          onChange={mudar('publico')}
          options={OPCOES_PUBLICO}
          placeholder="Escolha o público"
          required
          error={erros.publico}
          disabled={salvando}
        />
        <div className="flex flex-col-reverse justify-end gap-sm sm:flex-row">
          <button type="button" onClick={onClose} disabled={salvando} className={BOTAO_SECUNDARIO}>
            Cancelar
          </button>
          <Button type="submit" isLoading={salvando}>Salvar</Button>
        </div>
      </form>
    </Dialogo>
  );
}
