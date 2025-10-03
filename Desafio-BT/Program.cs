if (args.Length != 3)
{
    Console.WriteLine("Uso: <app> ATIVO PRECO_VENDA PRECO_COMPRA");
    return;
}

var ativo = args[0];
var precoVenda = args[1];
var precoCompra = args[2];

// DEBUG
Console.WriteLine($"Ativo: {ativo}");
Console.WriteLine($"Preço de Venda: {precoVenda}");
Console.WriteLine($"Preço de Compra: {precoCompra}");
