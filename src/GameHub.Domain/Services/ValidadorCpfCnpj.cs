namespace GameHub.Domain.Services;

/// <summary>
/// Validação de CPF/CNPJ pelos DÍGITOS VERIFICADORES — o algoritmo oficial, local e grátis
/// (não existe "API pra validar CPF": os 2 últimos dígitos são calculados a partir dos
/// anteriores; se a conta não bate, o documento é inválido). Regra pura de domínio:
/// sem estado, sem I/O — por isso é um static helper no Domain.
/// </summary>
public static class ValidadorCpfCnpj
{
    /// <summary>Deixa só os dígitos (aceita entrada com máscara: 123.456.789-09).</summary>
    public static string SoDigitos(string? texto) =>
        new((texto ?? string.Empty).Where(char.IsDigit).ToArray());

    public static bool ValidarCpf(string? cpf)
    {
        var d = SoDigitos(cpf);
        if (d.Length != 11) return false;
        if (d.All(c => c == d[0])) return false;   // 111.111.111-11 etc. passam na conta, mas são inválidos

        // 1º dígito verificador: soma dos 9 primeiros × pesos 10..2, resto por 11.
        var soma = 0;
        for (var i = 0; i < 9; i++) soma += (d[i] - '0') * (10 - i);
        var dv1 = soma % 11 < 2 ? 0 : 11 - soma % 11;
        if (dv1 != d[9] - '0') return false;

        // 2º dígito: soma dos 10 primeiros × pesos 11..2.
        soma = 0;
        for (var i = 0; i < 10; i++) soma += (d[i] - '0') * (11 - i);
        var dv2 = soma % 11 < 2 ? 0 : 11 - soma % 11;
        return dv2 == d[10] - '0';
    }

    public static bool ValidarCnpj(string? cnpj)
    {
        var d = SoDigitos(cnpj);
        if (d.Length != 14) return false;
        if (d.All(c => c == d[0])) return false;

        // CNPJ usa pesos cíclicos 2..9 (da direita pra esquerda).
        int[] pesos1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] pesos2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var soma = 0;
        for (var i = 0; i < 12; i++) soma += (d[i] - '0') * pesos1[i];
        var dv1 = soma % 11 < 2 ? 0 : 11 - soma % 11;
        if (dv1 != d[12] - '0') return false;

        soma = 0;
        for (var i = 0; i < 13; i++) soma += (d[i] - '0') * pesos2[i];
        var dv2 = soma % 11 < 2 ? 0 : 11 - soma % 11;
        return dv2 == d[13] - '0';
    }

    /// <summary>Formata para exibição: 123.456.789-09 ou 12.345.678/0001-95.</summary>
    public static string Formatar(string? cpfCnpj)
    {
        var d = SoDigitos(cpfCnpj);
        return d.Length switch
        {
            11 => $"{d[..3]}.{d[3..6]}.{d[6..9]}-{d[9..]}",
            14 => $"{d[..2]}.{d[2..5]}.{d[5..8]}/{d[8..12]}-{d[12..]}",
            _ => d
        };
    }
}
