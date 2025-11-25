// Copyright (c) Scott Doxey. All Rights Reserved. Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;



    public class HapticFeedbackController : MonoBehaviour
    {

        public void LightFeedback()
        {
            HapticFeedback.LightFeedback();
        }

        public void MediumFeedback()
        {
            HapticFeedback.MediumFeedback();
        }

        public void HeavyFeedback()
        {
            HapticFeedback.HeavyFeedback();
        }

    }


