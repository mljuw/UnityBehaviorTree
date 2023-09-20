using System;
using Codice.Client.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    public class VisualFadeBorder : VisualElement
    {
        public float fadeOutDuration = 2;
        public StyleColor sColor = new StyleColor(new Color(55/255f,55/255f,255/255f,1));
        
        private bool bDebugActivated = false;
        private float fadeOutCounter;
        private bool bEnableTick = false;
        public VisualFadeBorder()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            AddToClassList("fade-border");
        }

        public void SetActivated(bool activated)
        {
            if (bDebugActivated == activated) return;
            bDebugActivated = activated;
            if (bDebugActivated)
            {
                bEnableTick = true;
                AddToClassList("activated");
                
                var color = sColor.value;
                color.a = 1;
                sColor.value = color;
                SetBorderColor(ref sColor);
            }
            else
            {
                fadeOutCounter = fadeOutDuration;
            }
        }

        public void Blink()
        {
            AddToClassList("activated");
            bEnableTick = true;
            fadeOutCounter = fadeOutDuration;
        }

        public void Tick(float deltaTime)
        {
            if (bDebugActivated || !bEnableTick) return;

            fadeOutCounter -= deltaTime;
            if (fadeOutCounter <= 0)
            {
                fadeOutCounter = 0;
                bEnableTick = false;
                RemoveFromClassList("activated");
            }

            var color = sColor.value;
            color.a = fadeOutCounter / fadeOutDuration;
            sColor.value = color;
            SetBorderColor(ref sColor);

        }

        private void SetBorderColor(ref StyleColor color)
        {
            style.borderBottomColor = color;
            style.borderLeftColor = color;
            style.borderRightColor = color;
            style.borderTopColor = color;
        }
        
    }

    public class VisualDebugBorder : VisualElement
    {
        public StyleColor blinkColor = new (new Color(255/255f, 55/255f, 255/255f, 1));
        public StyleColor activatedColor = new StyleColor(new Color(55/255f, 55/255f, 255/255f, 1));
        
        public  VisualFadeBorder activatedBorder;
        public VisualFadeBorder blinkBorder;

        public VisualDebugBorder()
        {
            style.position = Position.Absolute;
            style.width = new Length(100, LengthUnit.Percent);
            style.height = new Length(100, LengthUnit.Percent);
            
            activatedBorder = new VisualFadeBorder
            {
                sColor = activatedColor
            };

            blinkBorder = new VisualFadeBorder
            {
                sColor = blinkColor
            };
            
            blinkBorder.AddToClassList("blink-border");
            Add(activatedBorder);
            Add(blinkBorder);

            pickingMode = PickingMode.Ignore;
        }
        
        public void SetActivated(bool activated)
        {
            activatedBorder.SetActivated(activated);
        }

        public void Blink()
        {
            blinkBorder.Blink();
        }

        public void Tick(float deltaTime)
        {
            activatedBorder.Tick(deltaTime);
            blinkBorder.Tick(deltaTime);
        }
    }
}