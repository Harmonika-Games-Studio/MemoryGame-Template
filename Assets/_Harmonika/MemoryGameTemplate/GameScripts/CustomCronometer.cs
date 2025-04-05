using Harmonika.Tools;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class CustomCronometer : MonoBehaviour, IThemeable {
    [SerializeField] TMP_Text _timerText;
    [SerializeField] TMP_Text _n1Text;
    [SerializeField] TMP_Text _n2Text;
    [SerializeField] TMP_Text _n3Text;
    [SerializeField] TMP_Text _n4Text;


    public int totalTimeInSeconds;
    public bool useFormat;
    public UnityEvent onEndTimer;

    private Coroutine _timerRoutine;
    private int _remainingTime;

    #region Properties

    public TMP_Text TimerText {
        get {
            return _timerText;
        }
    }

    #endregion

    public void StartTimer() {
        if (_timerRoutine != null)
            StopCoroutine(_timerRoutine);
        _timerRoutine = StartCoroutine(Timer());
    }

    public void EndTimer() {
        _timerText.text = "00:00";
        _remainingTime = 0;

        // Update N texts when timer ends
        UpdateNTexts(0);


        if (_timerRoutine != null)
            StopCoroutine(_timerRoutine);
    }

    public void ResetTimer() {
        EndTimer();
        _timerRoutine = StartCoroutine(Timer());
    }

    private IEnumerator Timer() {
        _remainingTime = totalTimeInSeconds;
        while (_remainingTime > 0) {
            int minutes = _remainingTime / 60;
            int seconds = _remainingTime % 60;

            if (useFormat) _timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            else _timerText.text = _remainingTime.ToString();

            UpdateNTexts(_remainingTime);

            yield return new WaitForSeconds(1);

            _remainingTime--;
        }
        onEndTimer?.Invoke();
    }

    private void UpdateNTexts(int remainingSeconds) {
        // Split the time into individual digits
        if (useFormat) {
            int minutes = remainingSeconds / 60;
            int seconds = remainingSeconds % 60;

            // Format: MM:SS (e.g., "05:42")
            char[] timeChars = string.Format("{0:00}:{1:00}", minutes, seconds).ToCharArray();

            // Update each N text with corresponding digit
            if (_n1Text != null) _n1Text.text = timeChars[0].ToString();
            if (_n2Text != null) _n2Text.text = timeChars[1].ToString();
            if (_n3Text != null) _n3Text.text = timeChars[3].ToString(); // Skip the colon at index 2
            if (_n4Text != null) _n4Text.text = timeChars[4].ToString();
        } else {
            // Just using the seconds count directly
            string timeStr = remainingSeconds.ToString().PadLeft(4, '0');

            // Update each N text with a digit from the time
            // For times less than 1000 seconds, the first digit will be 0
            for (int i = 0; i < timeStr.Length && i < 4; i++) {
                switch (i) {
                    case 0: if (_n1Text != null) _n1Text.text = timeStr[i].ToString(); break;
                    case 1: if (_n2Text != null) _n2Text.text = timeStr[i].ToString(); break;
                    case 2: if (_n3Text != null) _n3Text.text = timeStr[i].ToString(); break;
                    case 3: if (_n4Text != null) _n4Text.text = timeStr[i].ToString(); break;
                }
            }
        }
    }

    public void ChangeVisualIdentity() {
        throw new System.NotImplementedException();
    }
}
