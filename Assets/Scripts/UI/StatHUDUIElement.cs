using Stats;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class StatHUDUIElement : MonoBehaviour
{
    [SerializeField] private StatType _statTypeToShow;
    [SerializeField] private TextMeshProUGUI _statUIStatNameElement;
    [SerializeField] private TextMeshProUGUI _statUIValueElement;
    [SerializeField] private StatSheet _statSheet;

    [SerializeField] private bool _addPercentageSignAfterValue = false;

    private void Update()
    {
        //TODO: Make it so this only updates when the stat changes. This would have to be done within stat sheet.
        UpdateStatUI();
    }

    private void UpdateStatUI()
    {
        if (_statSheet == null) { return; }
        _statUIStatNameElement.text = $"{_statTypeToShow.HumanName()}:";
        string valueString = $"{_statSheet.GetTotal(_statTypeToShow)}";
        if (_addPercentageSignAfterValue) { valueString += "%"; }
        _statUIValueElement.text = valueString;
    }
}
