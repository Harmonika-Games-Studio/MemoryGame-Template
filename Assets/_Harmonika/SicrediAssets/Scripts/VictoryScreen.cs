using TMPro;
using UnityEngine;

public class VictoryScreen : MonoBehaviour {

    [SerializeField] public TMP_Text _prizeText;

    public void ChangePrizeText(string prizeText) {

        _prizeText.text = prizeText;
    }

}
