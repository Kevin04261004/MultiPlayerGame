using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject _inGameCanvas;
    [SerializeField] private GameObject _roomCanvas;
    [SerializeField] private TextMeshProUGUI _errorTMP;
    [field:SerializeField] public GameObject _loadingImage { get; private set; }
    [field:SerializeField] public TextMeshProUGUI _turnTMP { get; private set; }
    [SerializeField] private Slider _timeSlider;
    [SerializeField] private Image _fill;
    [SerializeField] private Transform[] playerPosArray;
    [SerializeField] private TextMeshProUGUI _codeTMP;
    private float _fadeTime = 3f;
    private Coroutine fadeCoroutine = null;
    private int minusPoint;
    [SerializeField] private TextMeshProUGUI _minusPointTMP;
    [field:SerializeField] public TextMeshProUGUI _roundTMP { get; private set; }
    public Button StartGameBtn;
    public Button ReadyGameBtn;
    public void ChangeCanvas()
    {
        if (_inGameCanvas.activeSelf)
        {
            _roomCanvas.SetActive(true);
            _inGameCanvas.SetActive(false);
        }
        else
        {
            _inGameCanvas.SetActive(true);
            _roomCanvas.SetActive(false);
        }
    }

    public void SetSlider(float a, float max)
    {
        _timeSlider.value = a / max;
        if (_timeSlider.value > 0.5f)
        {
            _fill.color = new Color(1- (_timeSlider.value-0.5f)*2, 1, 0);   
        }
        else
        {
            _fill.color = new Color(1, _timeSlider.value*2, 0);
        }
    }
    public void AddMinusPoint(int amount)
    {
        minusPoint += amount;
        _minusPointTMP.text = $"Minus Point: {minusPoint}";
    }

    public void ResetMinusPoint()
    {
        minusPoint = 0;
        _minusPointTMP.text = $"Minus Point: {minusPoint}";
    }
    public bool IsInGameCanvasActiveTrue()
    {
        return _inGameCanvas.activeSelf;
    }
    public void SetErrorTMP(string str)
    {
        _errorTMP.text = str;
    }

    public void SetGoodCodeTMP(string str)
    {
        _codeTMP.color = Color.green;
        _codeTMP.text = str;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        fadeCoroutine = StartCoroutine(FadeCodeTMPRoutine());
    }
    public void SetCodeTMP(string str)
    {
        _codeTMP.color = Color.red;
        _codeTMP.text = str;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        fadeCoroutine = StartCoroutine(FadeCodeTMPRoutine());
    }

    private IEnumerator FadeCodeTMPRoutine()
    {
        _codeTMP.color = new Color(_codeTMP.color.r,_codeTMP.color.g,_codeTMP.color.b,1);
        Color tempColor = _codeTMP.color;
        while (_codeTMP.color.a > 0)
        {
            tempColor.a -= Time.deltaTime/_fadeTime;
            _codeTMP.color = tempColor;
            yield return null;
        }
    }
    public Transform GetPlayerPosTransformOrNullFromPlayerPosArrayWithIndex(int index)
    {
        if (index >= playerPosArray.Length)
        {
            return null;
        }
        return playerPosArray[index];
    }

    public int GetMinusPoint()
    {
        return minusPoint;
    }
}
