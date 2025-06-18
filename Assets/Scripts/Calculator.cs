using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class Calculator : MonoBehaviour
{
    // Display elements
    [Header("Display")]
    [SerializeField] private TextMeshProUGUI _equationText;
    [SerializeField] private TextMeshProUGUI _resultText;

    [Header("History")]
    [SerializeField] private ScrollRect _historyScroll;
    [SerializeField] private RectTransform _historyContent;
    [SerializeField] private TextMeshProUGUI _historyItemPrefab;

    // Runtime state
    private  List<string> _tokens = new();
    private  StringBuilder _inputBuffer = new();
    private  List<string> _history = new();

    private bool _justEvaluated;

    // Button references set via Inspector
    [Header("Buttons")]
    [SerializeField] private Button[] _numberButtons = new Button[10]; // 0-9
    [SerializeField] private Button _buttonDot;

    [SerializeField] private Button _buttonAdd;
    [SerializeField] private Button _buttonSub;
    [SerializeField] private Button _buttonMult;
    [SerializeField] private Button _buttonDiv;

    [SerializeField] private Button _buttonAC;
    [SerializeField] private Button _buttonEq;
    [SerializeField] private Button _buttonRev;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _clickClip;
    [SerializeField] private AudioClip _hoverClip;

    private int MaxHistoryEntries = 30;

    // Internal helpers
    private VerticalLayoutGroup _historyLayoutGroup;

    // Set up references and default state
    private void OnEnable()
    {
        EnsureHistoryLayout();
        HookUpButtons();
        ClearAll();
        _justEvaluated = false;
    }

    // Wire calculator buttons to handlers
    private void HookUpButtons()
    {
        // Number buttons 0-9
        for (int i = 0; i < _numberButtons.Length && i < 10; i++)
        {
            int digit = i;
            if (_numberButtons[digit] != null)
            {
                _numberButtons[digit].onClick.AddListener(() => OnNumberClicked(digit.ToString()));
                AttachHoverSound(_numberButtons[digit]);
            }
        }
        if (_buttonDot != null)
        {
            _buttonDot.onClick.AddListener(() => OnNumberClicked("."));
            AttachHoverSound(_buttonDot);
        }

        // Operators
        if (_buttonAdd != null)  { _buttonAdd.onClick.AddListener(() => OnOperatorClicked("+")); AttachHoverSound(_buttonAdd);}
        if (_buttonSub != null)  { _buttonSub.onClick.AddListener(() => OnOperatorClicked("-")); AttachHoverSound(_buttonSub);}
        if (_buttonMult != null) { _buttonMult.onClick.AddListener(() => OnOperatorClicked("×")); AttachHoverSound(_buttonMult);}
        if (_buttonDiv != null)  { _buttonDiv.onClick.AddListener(() => OnOperatorClicked("÷")); AttachHoverSound(_buttonDiv);}

        // Functional buttons
        if (_buttonAC != null) { _buttonAC.onClick.AddListener(ClearAll); AttachHoverSound(_buttonAC);}
        if (_buttonEq != null) { _buttonEq.onClick.AddListener(Evaluate); AttachHoverSound(_buttonEq);}
        if (_buttonRev != null) { _buttonRev.onClick.AddListener(ToggleSign); AttachHoverSound(_buttonRev);}
    }

    #region Buttons

    // Handle numeric input
    private void OnNumberClicked(string value)
    {
        PlayClick();
        if (_justEvaluated)
        {
            _tokens.Clear();
            _inputBuffer.Clear();
            _justEvaluated = false;
        }

        if (value == "." && _inputBuffer.ToString().Contains(".")) return;

        _inputBuffer.Append(value);
        UpdateEquationLabel();
    }

    // Handle operator input
    private void OnOperatorClicked(string op)
    {
        PlayClick();
        if (_justEvaluated) _justEvaluated = false;
        if (_inputBuffer.Length == 0 && _tokens.Count == 0) return;

        if (_inputBuffer.Length > 0)
        {
            _tokens.Add(_inputBuffer.ToString());
            _inputBuffer.Clear();
        }
        if (_tokens.Count > 0 && IsOperator(_tokens.Last())) _tokens[_tokens.Count - 1] = op;
        else _tokens.Add(op);

        UpdateEquationLabel();
    }

    // Toggle sign of current/last number
    private void ToggleSign()
    {
        PlayClick();
        if (_inputBuffer.Length > 0)
        {
            if (_inputBuffer[0] == '-') _inputBuffer.Remove(0, 1);
            else _inputBuffer.Insert(0, "-");
        }
        else if (_tokens.Count > 0 && !IsOperator(_tokens.Last()))
        {
            string lastNum = _tokens.Last();
            _tokens[_tokens.Count - 1] = lastNum.StartsWith("-") ? lastNum.Substring(1) : "-" + lastNum;
        }
        UpdateEquationLabel();
    }

    // Reset calculator state
    private void ClearAll()
    {
        PlayClick();
        _tokens.Clear();
        _inputBuffer.Clear();
        _justEvaluated = false;
        if (_equationText != null) _equationText.text = "0";
        if (_resultText   != null) _resultText.text   = "0";
    }

    // Evaluate expression and update history
    private void Evaluate()
    {
        PlayClick();
        if (_inputBuffer.Length > 0)
        {
            _tokens.Add(_inputBuffer.ToString());
            _inputBuffer.Clear();
        }
        if (_tokens.Count == 0 || IsOperator(_tokens.Last())) return;

        var expressionSnapshot = string.Join(" ", _tokens);

        if (!TryEvaluateTokens(_tokens, out double result) || double.IsNaN(result) || double.IsInfinity(result))
        {
            if (_resultText != null) _resultText.text = "Error";
            return;
        }

        if (_resultText != null) _resultText.text = FormatNumber(result);
        AddToHistory($"{expressionSnapshot} = {FormatNumber(result)}");

        _tokens.Clear();
        _tokens.Add(result.ToString());
        UpdateEquationLabel();
        _justEvaluated = true;
        FitResultFont();
    }

    #endregion


    #region Helpers

    // Refresh equation label and auto-scroll
    private void UpdateEquationLabel()
    {
        if (_equationText == null) return;

        var sb = new StringBuilder();
        foreach (var t in _tokens) sb.Append(t).Append(' ');
        if (_inputBuffer.Length > 0) sb.Append(_inputBuffer);
        if (sb.Length == 0) sb.Append('0');
        _equationText.text = sb.ToString();
    }

    // Check if token is operator
    private static bool IsOperator(string s)
    {
        return s == "+" || s == "-" || s == "×" || s == "*" || s == "÷" || s == "/";
    }

    // Evaluate token list respecting precedence
    private bool TryEvaluateTokens(List<string> tokens, out double result)
    {
        var work = new List<string>(tokens);
        for (int i = 0; i < work.Count; ++i)
        {
            if (work[i] == "×" || work[i] == "*" || work[i] == "÷" || work[i] == "/")
            {
                if (!double.TryParse(work[i - 1], out double lhs) || !double.TryParse(work[i + 1], out double rhs))
                {
                    result = double.NaN;
                    return false;
                }
                if ((work[i] == "÷" || work[i] == "/") && Mathf.Approximately((float)rhs, 0f))
                {
                    result = double.NaN;
                    return false;
                }
                double sub = work[i] == "×" || work[i] == "*" ? lhs * rhs : lhs / rhs;
                work[i - 1] = sub.ToString();
                work.RemoveAt(i);
                work.RemoveAt(i);
                i--;
            }
        }
        while (work.Count > 1)
        {
            if (!double.TryParse(work[0], out double lhs) || !double.TryParse(work[2], out double rhs))
            {
                result = double.NaN;
                return false;
            }
            double sub = work[1] == "+" ? lhs + rhs : lhs - rhs;
            work[0] = sub.ToString();
            work.RemoveAt(1);
            work.RemoveAt(1);
        }
        return double.TryParse(work[0], out result);
    }

    // Format number for display
    private static string FormatNumber(double value)
    {
        return value.ToString("G15");
    }

    // Append entry to history view
    private void AddToHistory(string entry)
    {
        if (_historyScroll == null || _historyContent == null || _historyItemPrefab == null) return;

        _history.Add(entry);
        if (_history.Count > MaxHistoryEntries)
        {
            _history.RemoveAt(0);
            if (_historyContent.childCount > 0) Destroy(_historyContent.GetChild(0).gameObject);
        }

        var label = Instantiate(_historyItemPrefab, _historyContent);
        label.text = entry;

        // Rebuild layout to update content height and scroll to bottom
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_historyContent);
        _historyScroll.verticalNormalizedPosition = 0f;
    }

    // Shrink result text to fit label width
    private void FitResultFont()
    {
        if (_resultText == null) return;
        float available = _resultText.rectTransform.rect.width - 4f;
        if (available <= 0) return;
        int fontSize = 60;
        while (fontSize >= 20)
        {
            if (_resultText.text.Length * fontSize * 0.55f <= available) break;
            fontSize -= 2;
        }
        _resultText.fontSize = fontSize;
    }

    // Make sure the history content has the necessary layout components for proper stacking and scrolling
    private void EnsureHistoryLayout()
    {
        if (_historyContent == null) return;

        // Add VerticalLayoutGroup if missing
        if (!_historyContent.TryGetComponent(out _historyLayoutGroup))
        {
            _historyLayoutGroup = _historyContent.gameObject.AddComponent<VerticalLayoutGroup>();
            _historyLayoutGroup.childControlHeight = true;
            _historyLayoutGroup.childForceExpandHeight = false;
            _historyLayoutGroup.childControlWidth = true;
            _historyLayoutGroup.childForceExpandWidth = true;
            _historyLayoutGroup.spacing = 4f;
            _historyLayoutGroup.childAlignment = TextAnchor.LowerLeft;
        }

        // Add ContentSizeFitter so content height grows with items
        if (!_historyContent.TryGetComponent(out ContentSizeFitter fitter))
        {
            fitter = _historyContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        // Ensure the RectTransform anchors/pivot keep the content pinned to the bottom of the ScrollRect
        _historyContent.pivot = new Vector2(0.5f, 0f);
        _historyContent.anchorMin = new Vector2(0f, 0f);
        _historyContent.anchorMax = new Vector2(1f, 0f);
    }

    #endregion

    #region Audio

    private void PlayClick()
    {
        if (_audioSource == null || _clickClip == null) return;
        _audioSource.PlayOneShot(_clickClip);
    }

    private void PlayHover()
    {
        if (_audioSource == null || _hoverClip == null) return;
        _audioSource.PlayOneShot(_hoverClip);
    }

    private void AttachHoverSound(Button btn)
    {
        if (btn == null) return;
        var trigger = btn.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        var entry = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
        };
        entry.callback.AddListener((_) => PlayHover());
        trigger.triggers.Add(entry);
    }

    #endregion
}