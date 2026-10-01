
// Cristian Pop - https://boxophobic.com/

using UnityEngine;
using Boxophobic.StyledGUI;
using System.Collections;

namespace PolyverseSkiesAsset
{
    [HelpURL("https://docs.google.com/document/d/1z7A_xKNa2mXhvTRJqyu-ZQsAtbV32tEZQbO1OmPS_-s/edit?usp=sharing")]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class PolyverseSkies : StyledMonoBehaviour
    {
        [StyledBanner(0.968f, 0.572f, 0.890f, "Polyverse Skies")]
        public bool styledBanner;

        [StyledCategory("Scene", 5, 10)]
        public bool categoryScene;

        public GameObject sunDirection;
        public GameObject moonDirection;

        [StyledCategory("Time Of Day")]
        public bool categoryTime;

        [StyledMessage(
            "Info",
            "The Time Of Day feature will interpolate between two Polyverse Skies materials. Please note that material properties such as textures and keywords will not be interpolated! You will need to enable the same features on both materials in order for the interpolation to work! Toggle Update Lighting to enable Unity's realtime environment lighting!",
            0,
            10
        )]
        public bool categoryTimeMessage = true;

        public Material skyboxDay;
        public Material skyboxNight;

        [Range(0, 1)]
        public float timeOfDay = 0;

        [Space(10)]
        public bool updateLighting = false;

        [StyledSpace(5)]
        public bool styledSpace0;

        // =========================================================
        // AUTOMATIC TIME OF DAY
        // =========================================================

        [Header("Automatic Time Of Day")]

        [Tooltip("Time in seconds for Time Of Day to move from 0 to 1, and also from 1 back to 0.")]
        [Min(0.1f)]
        public float timeCycleDuration = 5f;

        [Tooltip("Automatically start the day/night cycle when the game starts.")]
        public bool automaticTimeOfDay = true;

        private Material skyboxMaterial;

        private Coroutine timeOfDayCoroutine;


        // =========================================================
        // START
        // =========================================================

        private void Start()
        {
            CreateSkyboxMaterial();

            if (automaticTimeOfDay)
            {
                StartTimeOfDayCycle();
            }
        }


        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            // -----------------------------------------------------
            // SUN
            // -----------------------------------------------------

            if (sunDirection != null)
            {
                Shader.SetGlobalVector(
                    "GlobalSunDirection",
                    -sunDirection.transform.forward
                );
            }
            else
            {
                Shader.SetGlobalVector(
                    "GlobalSunDirection",
                    Vector3.zero
                );
            }


            // -----------------------------------------------------
            // MOON
            // -----------------------------------------------------

            if (moonDirection != null)
            {
                Shader.SetGlobalVector(
                    "GlobalMoonDirection",
                    -moonDirection.transform.forward
                );
            }
            else
            {
                Shader.SetGlobalVector(
                    "GlobalMoonDirection",
                    Vector3.zero
                );
            }


            // -----------------------------------------------------
            // SKYBOX
            // -----------------------------------------------------

            if (skyboxDay != null && skyboxNight != null)
            {
                if (skyboxMaterial == null)
                {
                    CreateSkyboxMaterial();
                }

                if (skyboxMaterial != null)
                {
                    skyboxMaterial.Lerp(
                        skyboxDay,
                        skyboxNight,
                        timeOfDay
                    );

                    RenderSettings.skybox = skyboxMaterial;
                }
            }


            // -----------------------------------------------------
            // REALTIME LIGHTING
            // -----------------------------------------------------

            if (updateLighting)
            {
                DynamicGI.UpdateEnvironment();
            }
        }


        // =========================================================
        // CREATE SKYBOX MATERIAL
        // =========================================================

        private void CreateSkyboxMaterial()
        {
            if (skyboxDay != null && skyboxNight != null)
            {
                skyboxMaterial = new Material(skyboxDay);
            }
        }


        // =========================================================
        // START TIME OF DAY CYCLE
        // =========================================================

        public void StartTimeOfDayCycle()
        {
            StopTimeOfDayCycle();

            timeOfDayCoroutine = StartCoroutine(
                TimeOfDayCycle()
            );
        }


        // =========================================================
        // STOP TIME OF DAY CYCLE
        // =========================================================

        public void StopTimeOfDayCycle()
        {
            if (timeOfDayCoroutine != null)
            {
                StopCoroutine(timeOfDayCoroutine);
                timeOfDayCoroutine = null;
            }
        }


        // =========================================================
        // TIME OF DAY CYCLE
        // =========================================================

        private IEnumerator TimeOfDayCycle()
        {
            while (true)
            {
                // -------------------------------------------------
                // DAY -> NIGHT
                // 0 -> 1
                // -------------------------------------------------

                yield return StartCoroutine(
                    ChangeTimeOfDay(
                        0f,
                        1f,
                        timeCycleDuration
                    )
                );


                // -------------------------------------------------
                // NIGHT -> DAY
                // 1 -> 0
                // -------------------------------------------------

                yield return StartCoroutine(
                    ChangeTimeOfDay(
                        1f,
                        0f,
                        timeCycleDuration
                    )
                );
            }
        }


        // =========================================================
        // CHANGE TIME OF DAY
        // =========================================================

        private IEnumerator ChangeTimeOfDay(
            float startValue,
            float endValue,
            float duration
        )
        {
            float elapsed = 0f;

            duration = Mathf.Max(
                0.1f,
                duration
            );


            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / duration
                );


                timeOfDay = Mathf.Lerp(
                    startValue,
                    endValue,
                    progress
                );


                yield return null;
            }


            // Make sure the exact final value is reached.
            timeOfDay = endValue;
        }


        // =========================================================
        // MANUAL RESET
        // =========================================================

        public void ResetTimeOfDay()
        {
            StopTimeOfDayCycle();

            timeOfDay = 0f;
        }


//#if UNITY_EDITOR

//        private void OnValidate()
//        {
//            if (skyboxDay != null && skyboxNight != null)
//            {
//                skyboxMaterial = new Material(skyboxDay);
//            }

//            timeCycleDuration = Mathf.Max(
//                0.1f,
//                timeCycleDuration
//            );
//        }

//#endif
    }
}
