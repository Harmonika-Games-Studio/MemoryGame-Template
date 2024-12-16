using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MemoryGameCard : MonoBehaviour
{
    [SerializeField] private Image _cardImage;
    public Sprite cardFront, cardBack;

    private bool _coroutineAllowed;
    private bool _facedUp;
    private bool isCorect = false;
    [HideInInspector] public MemoryGame manager;
    [HideInInspector] public int id;

    public bool IsCorect
    {
        get => isCorect;
        set
        {
            if (!value) Invoke("RotateCardDown", 1); 
            isCorect = value;
        }
    }

    void Start()
    {
        StartFacedUp(false);
        _coroutineAllowed = true;
        RotateCard();
    }

    public void ClickOnCard()
    {
        if (!manager.CanClick) return;
        RotateCard();
        manager.ClickedOnCard(this);
    }

    public void RotateCard()
    {
        if (_coroutineAllowed)
            StartCoroutine(RotateCardRoutine());
    }
    
    public void RotateCardDown()
    {
        if (_facedUp) RotateCard();
    }

    private void StartFacedUp(bool value = true)
    {
        if (_facedUp) _cardImage.sprite = cardFront;
        else _cardImage.sprite = cardBack;

        _facedUp = value;
    }

    IEnumerator RotateCardRoutine() //TODO: Corrigir bug onde a carta perde a capacidade de ouvir click quando virada para baixo
    {
        _coroutineAllowed = false;

        float startAngle = _facedUp ? 180 : 0;
        float endAngle = _facedUp ? 0 : 180;
        Sprite newSprite = _facedUp ? cardBack : cardFront;

        for (float i = startAngle; (_facedUp ? i >= endAngle : i <= endAngle); i += (_facedUp ? -10 : 10))
        {
            transform.rotation = Quaternion.Euler(0, i, 0);
            if (i == 90)
            {
                _cardImage.sprite = newSprite;

                Vector3 scale = _cardImage.rectTransform.localScale;
                scale.x = !_facedUp ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
                _cardImage.rectTransform.localScale = scale;
            }

            yield return new WaitForSeconds(.01f);
        }

        _facedUp = !_facedUp;
        _coroutineAllowed = true;
    }
}