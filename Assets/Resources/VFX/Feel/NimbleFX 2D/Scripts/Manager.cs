
using TMPro;
using UnityEngine;

namespace BhanuProductions.NimbleFX
{
    public class Manager : MonoBehaviour
    {
        public SpriteAnimations[] Animations;
        public UIAnimations[] UIAnimations;
        public bool Looping = false;
        public TMP_Text LoopingText;




        public void PlayEffect()
        {
            foreach (SpriteAnimations anim in Animations)
            {
                anim.StartSelectedEffect();
            }

        }

        public void ToggleLooping()
        {
            if (Looping)
            {
                Looping = false;
                foreach (SpriteAnimations anim in Animations)
                {
                    anim.loop = false;
                }
                LoopingText.text = "Looping : off";
            }
            else
            {
                Looping = true;
                foreach (SpriteAnimations anim in Animations)
                {
                    anim.loop = true;
                }
                LoopingText.text = "Looping : on";
            }
        }

        public void StopAnimations()
        {
            foreach (SpriteAnimations anim in Animations)
            {
                anim.StopSelectedEffect();
            }
        }

        public void PlayEffectUI()
        {
            foreach (UIAnimations anim in UIAnimations)
            {
                anim.StartSelectedEffect();
            }

        }

        public void ToggleLoopingUI()
        {
            if (Looping)
            {
                Looping = false;
                foreach (UIAnimations anim in UIAnimations)
                {
                    anim.loop = false;
                }
                LoopingText.text = "Looping : off";
            }
            else
            {
                Looping = true;
                foreach (UIAnimations anim in UIAnimations)
                {
                    anim.loop = true;
                }
                LoopingText.text = "Looping : on";
            }
        }

        public void StopAnimationsUI()
        {
            foreach (UIAnimations anim in UIAnimations)
            {
                anim.StopSelectedEffect();
            }
        }

    }
}