#nullable enable
using UnityEngine;

namespace CrowdDefense.Visual
{
    // Port de AnimationController.js (Three.js AnimationMixer) → Unity Mechanim Animator.
    // Wrapper stateless : opère sur l'Animator trouvé dans le subtree du meshRoot.
    // Phase 3.C : states Idle + Walk via bool isWalking.
    // Phase 3.C+ : Attack (fireTrigger) + Death (dieTrigger) ajoutés MIGRATE-VISUAL-09/10.
    public static class AnimationController
    {
        // Convention : controllers dans Assets/Resources/Animations/Controllers/{name}.controller
        // Générés par l'Editor tool BuildAnimatorControllers (Assets/Editor/).
        private const string k_ControllerBasePath = "Animations/Controllers/";

        /// <summary>
        /// Crée ou récupère l'Animator sur meshRoot et assigne le RuntimeAnimatorController
        /// depuis Resources si disponible.
        /// idleClipName / walkClipName : hint pour log uniquement à ce stade (assignation
        /// dans .controller géré par BuildAnimatorControllers Editor tool).
        /// Retourne null silencieusement si pas de controller trouvé (fallback safe).
        /// </summary>
        public static Animator? SetupAnimator(
            GameObject meshRoot,
            string? idleClipName,
            string? walkClipName)
        {
            var animator = meshRoot.GetComponent<Animator>()
                ?? meshRoot.AddComponent<Animator>();

            string controllerPath = k_ControllerBasePath + meshRoot.name;
            var controller = Resources.Load<RuntimeAnimatorController>(controllerPath);

            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"[AnimationController] Pas de controller pour '{meshRoot.name}' " +
                    $"à Resources/{controllerPath} — run 'Tools/CrowdDefense/Build Animator Controllers'.");
#endif
            }

            return animator;
        }

        /// <summary>
        /// Joue un state par nom via Animator.Play — cross-fade 0.2s (équivalent fadeIn Phaser).
        /// No-op si anim null ou state inexistant.
        /// </summary>
        public static void Play(Animator? anim, string stateName, float fadeDuration = 0.2f)
        {
            if (anim == null) return;
            anim.CrossFade(stateName, fadeDuration);
        }

        /// <summary>
        /// Toggle de l'état Walk via bool isWalking (Mechanim parameter).
        /// Utilisé par Enemy.Update chaque frame (SetBool est O(1) Mechanim).
        /// </summary>
        public static void SetWalking(Animator? anim, bool walking)
        {
            if (anim == null) return;
            anim.SetBool("isWalking", walking);
        }

        /// <summary>
        /// Déclenche le trigger attackTrigger (Tower.Fire, Enemy attack).
        /// </summary>
        public static void TriggerAttack(Animator? anim)
        {
            if (anim == null) return;
            anim.SetTrigger("attackTrigger");
        }

        /// <summary>
        /// Déclenche le trigger dieTrigger (Enemy.TakeDamage fatal).
        /// </summary>
        public static void TriggerDeath(Animator? anim)
        {
            if (anim == null) return;
            anim.SetTrigger("dieTrigger");
        }
    }
}
