using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRogue
{
    public class UIStateController : MonoBehaviour
    {
        [SerializeField] private List<UIStateEntry> states = new List<UIStateEntry>();

        public void Show(string stateId)
        {
            foreach (var entry in states)
            {
                if (entry.Panel != null)
                {
                    entry.Panel.SetActive(string.Equals(entry.Id, stateId, StringComparison.Ordinal));
                }
            }
        }

        public bool HasState(string stateId)
        {
            foreach (var entry in states)
            {
                if (string.Equals(entry.Id, stateId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public class UIStateEntry
    {
        public string Id;
        public GameObject Panel;
    }
}
