namespace AuditorFiscal.UI.Messaging;

/// <summary>
/// Enviada sempre que uma O.S. é criada, alterada, excluída ou tem situação/favorito
/// alterado, para que telas como o Cronograma e o Banco de Dados se recarreguem sozinhas
/// mesmo quando a alteração foi feita a partir de outra tela.
/// </summary>
public sealed class OrdemServicoAlteradaMessage;
