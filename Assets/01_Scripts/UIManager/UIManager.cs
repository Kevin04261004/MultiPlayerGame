using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject _inGameCanvas;
    [SerializeField] private GameObject _roomCanvas;
    [SerializeField] private TextMeshProUGUI _errorTMP;
    [field:SerializeField] public GameObject _loadingImage { get; private set; }
    [field:SerializeField] public TextMeshProUGUI _turnTMP { get; private set; }
    [field:SerializeField] public TextMeshProUGUI _timeTMP { get; private set; }
    [SerializeField] private Transform[] playerPosArray;
    [SerializeField] private TextMeshProUGUI _codeTMP;
    private float _fadeTime = 3f;
    private Coroutine fadeCoroutine = null;
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
}
