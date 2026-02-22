using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class UILevelCompletedAnimEvents : MonoBehaviour
{
    [SerializeField] private List<UICounterTextTween> counters = new List<UICounterTextTween>();

    public void PlayCounterByIndex(int index)
    {
        if (index < 0 || index >= counters.Count)
        {
            return;
        }

        UICounterTextTween counter = counters[index];
        if (counter == null)
        {
            return;
        }

        counter.Play();
    }

    public void PlayAllCounters()
    {
        for (int i = 0; i < counters.Count; i++)
        {
            UICounterTextTween counter = counters[i];
            if (counter == null)
            {
                continue;
            }

            counter.Play();
        }
    }
}
