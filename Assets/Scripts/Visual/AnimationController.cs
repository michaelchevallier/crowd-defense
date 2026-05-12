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

            // meshRoot.name may be "Mesh_knight" — strip "Mesh_" prefix, try exact then lowercase
            string rawName = meshRoot.name;
            if (rawName.StartsWith("Mesh_", System.StringComparison.OrdinalIgnoreCase))
                rawName = rawName.Substring(5);

            var controller =
                Resources.Load<RuntimeAnimatorController>(k_ControllerBasePath + rawName)
                ?? Resources.Load<RuntimeAnimatorController>(k_ControllerBasePath + rawName.ToLowerInvariant());

            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"[AnimationController] Pas de controller pour '{rawName}' " +
                    $"à Resources/{k_ControllerBasePath} — run 'Tools/CrowdDefense/Build Animator Controllers'.");
#endif
            }

            return animator;
        }

        /// <summary>
        /// Toggle de l'état Walk via bool isWalking (Mechanim parameter).
        /// Utilisé par Enemy.Update et Hero.Update chaque frame (SetBool est O(1) Mechanim).
        /// </summary>
        public static void SetWalking(Animator? anim, bool walking)
        {
            if (anim == null) return;
            anim.SetBool("isWalking", walking);
        }

        /// <summary>
        /// Déclenche la transition vers l'état Attack via attackTrigger.
        /// Le controller revient vers Idle automatiquement (exitTime 0.9 → Idle transition).
        /// </summary>
        public static void TriggerAttack(Animator? anim)
        {
            if (anim == null) return;
            anim.SetTrigger("attackTrigger");
        }

        /// <summary>
        /// Force le retour vers Idle : reset isWalking + reset attackTrigger en attente.
        /// À appeler quand l'entité passe de combat → repos sans mouvement.
        /// </summary>
        public static void SetIdle(Animator? anim)
        {
            if (anim == null) return;
            anim.SetBool("isWalking", false);
            anim.ResetTrigger("attackTrigger");
        }

    }
}
