using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Harmonika.Tools
{
    public class CustomToggleGroup : MonoBehaviour
    {
        [SerializeField] private List<Toggle> toggleList;

        private void Start()
        {
            foreach (Toggle toggle in toggleList)
            {
                toggle.onValueChanged.AddListener((bool value) =>
                {
                    if (value)
                    {
                        SetFalseAllOtherToggles(toggle);
                    }
                    else
                    {
                        if (!AnyOtherToggleIsActive())
                        {
                            toggle.isOn = true;
                        }
                    }
                });
            }
        }

        private void SetFalseAllOtherToggles(Toggle activeToggle)
        {
            foreach (Toggle toggle in toggleList)
            {
                if (activeToggle != toggle)
                {
                    toggle.isOn = false;
                }
            }
        }

        private bool AnyOtherToggleIsActive()
        {
            foreach (Toggle toggle in toggleList)
            {
                if (toggle.isOn)
                {
                    return true;
                }
            }
            return false;
        }
    }
}