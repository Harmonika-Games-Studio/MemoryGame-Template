using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using Harmonika.Tools;

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
                input.text = input.text.Format(ParseableFields.phone);
                break;
            case stringFormat.CPF:
                input.text = input.text.Format(ParseableFields.cpf);
                break;
            case stringFormat.DATE:
                input.text = input.text.Format(ParseableFields.date);
                break;
        }
    }
}
