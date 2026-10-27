// Dados ilustrativos para a primeira entrega visual. Nenhum item representa um envio real.
export const SENT_HISTORY_REFERENCE_TIME = Date.now();

const daysAgo = (days, hours = 0) =>
  new Date(SENT_HISTORY_REFERENCE_TIME - days * 86_400_000 - hours * 3_600_000).toISOString();

export const SENT_HISTORY_MOCK = [
  { id: 'INV-1042', title: 'Atualização da base de dados', detail: 'Release de exemplo', sentAt: daysAgo(0, 2), tone: 'Técnico', status: 'Enviado' },
  { id: 'INV-1041', title: 'Integração de novos clientes', detail: 'Cliente de exemplo', sentAt: daysAgo(0, 5), tone: 'Formal', status: 'Pendente' },
  { id: 'INV-1038', title: 'Correção na autenticação', detail: 'Prioridade alta', sentAt: daysAgo(1, 4), tone: 'Simplificado', status: 'Falhou' },
  { id: 'INV-1035', title: 'Relatório financeiro trimestral', detail: 'Comunicado de exemplo', sentAt: daysAgo(2, 1), tone: 'Formal', status: 'Enviado' },
  { id: 'INV-1033', title: 'Melhorias no painel de controle', detail: 'Release de exemplo', sentAt: daysAgo(3, 2), tone: 'Simplificado', status: 'Enviado' },
  { id: 'INV-1030', title: 'Ajustes na emissão de notas', detail: 'Cliente de exemplo', sentAt: daysAgo(4, 3), tone: 'Técnico', status: 'Pendente' },
  { id: 'INV-1028', title: 'Nova consulta de documentos', detail: 'Release de exemplo', sentAt: daysAgo(5, 1), tone: 'Formal', status: 'Enviado' },
  { id: 'INV-1025', title: 'Correção na busca de registros', detail: 'Comunicado de exemplo', sentAt: daysAgo(6, 5), tone: 'Simplificado', status: 'Falhou' },
  { id: 'INV-1022', title: 'Melhorias no cadastro', detail: 'Release de exemplo', sentAt: daysAgo(12, 2), tone: 'Técnico', status: 'Enviado' },
  { id: 'INV-1019', title: 'Atualização de permissões', detail: 'Cliente de exemplo', sentAt: daysAgo(18, 4), tone: 'Formal', status: 'Enviado' },
  { id: 'INV-1015', title: 'Ajustes na exportação', detail: 'Release de exemplo', sentAt: daysAgo(35, 2), tone: 'Simplificado', status: 'Pendente' },
  { id: 'INV-1010', title: 'Revisão do fluxo de cadastro', detail: 'Comunicado de exemplo', sentAt: daysAgo(70, 3), tone: 'Formal', status: 'Enviado' },
];
