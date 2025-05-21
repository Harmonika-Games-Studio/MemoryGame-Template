using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;

[System.Serializable]
public enum stringFormat
{
    PHONE,
    CPF,
    DATE
}

public class StringFormater : MonoBehaviour
{
    [SerializeField] TMP_InputField input;
    [SerializeField] stringFormat format;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        input.onSubmit.AddListener(Format);
    }

    private void Format(string text)
    {
        switch (format)
        {
            case stringFormat.PHONE:
                input.text = FormatPhone(text);
                break;
            case stringFormat.CPF:
                input.text = FormatCPF(text);
                break;
            case stringFormat.DATE:
                input.text = FormatDate(text);
                break;
        }
    }

    private string FormatCPF(string cpf)
    {
        if (cpf.Length == 11)
        {
            return Regex.Replace(cpf, @"(\d{3})(\d{3})(\d{3})(\d{2})", "$1.$2.$3-$4");
        }
        return cpf; // Retorna sem formatação se não tiver o tamanho esperado
    }

    private string FormatPhone(string phone)
    {
        if (phone.Length == 11) // Formato 9 dígitos (Brasil)
        {
            return Regex.Replace(phone, @"(\d{2})(\d{5})(\d{4})", "($1) $2-$3");
        }
        else if (phone.Length == 10) // Formato 8 dígitos
        {
            return Regex.Replace(phone, @"(\d{2})(\d{4})(\d{4})", "($1) $2-$3");
        }
        return phone; // Retorna sem formatação se não tiver o tamanho esperado
    }

    private string FormatDate(string birthDate)
    {
        if (birthDate.Length == 8) // Formato esperado: DDMMYYYY
        {
            return Regex.Replace(birthDate, @"(\d{2})(\d{2})(\d{4})", "$1/$2/$3");
        }
        return birthDate; // Retorna sem formatação se não tiver o tamanho esperado
    }
}
