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
            // meshRoot.name may be "Mesh_knight" — strip "Mesh_" prefix, try exact then lowercase
            string rawName = meshRoot.name;
            if (rawName.StartsWith("Mesh_", System.StringComparison.OrdinalIgnoreCase))
                rawName = rawName.Substring(5);

            var controller =
                Resources.Load<RuntimeAnimatorController>(k_ControllerBasePath + rawName)
                ?? Resources.Load<RuntimeAnimatorController>(k_ControllerBasePath + rawName.ToLowerInvariant());

            // KayKit characters: the GLB root node is named "Rig_Medium" but UnityGLTF renames
            // the prefab root to the file name (e.g. "Knight"). Their animation clips use paths
            // "Rig_Medium/root/hips/..." so the Animator must be on the PARENT of meshRoot,
            // and meshRoot must be renamed to "Rig_Medium" to match the binding paths.
            // Detection: meshRoot has a direct child named "root" (KayKit rig structure)
            // but no direct child named "CharacterArmature" (Quaternius rig structure).
            bool isKayKit = meshRoot.transform.Find("root") != null
                         && meshRoot.transform.Find("CharacterArmature") == null;

            GameObject animatorTarget;
            if (isKayKit && meshRoot.transform.parent != null)
            {
                // Rename meshRoot to match the clip binding prefix "Rig_Medium"
                meshRoot.name = "Rig_Medium";
                animatorTarget = meshRoot.transform.parent.gameObject;
            }
            else
            {
                animatorTarget = meshRoot;
            }

            if (!animatorTarget.TryGetComponent(out Animator animator))
            {
                animator = animatorTarget.AddComponent<Animator>();
                if (animator == null) return null;
            }

            // Disable root motion to avoid sliding animations
            animator.applyRootMotion = false;

            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }
            else
            {
                // Fallback to BaseCharacter generic controller so SkinnedMeshRenderer doesn't
                // render magenta (URP default when shader skinning has no animator binding).
                var fallback = Resources.Load<RuntimeAnimatorController>(k_ControllerBasePath + "BaseCharacter");
                if (fallback != null)
                {
                    animator.runtimeAnimatorController = fallback;
                }
                else
                {
                    // V6 W1-Y: BaseCharacter controller also absent — disable all SkinnedMeshRenderers
                    // to avoid magenta placeholder cubes on URP.
                    foreach (var smr in animatorTarget.GetComponentsInChildren<SkinnedMeshRenderer>())
                        smr.enabled = false;
                }
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"[AnimationController] Controller '{rawName}' missing — fallback BaseCharacter applied. " +
                    $"Run 'Tools/CrowdDefense/Build Animator Controllers' to generate per-type controllers.");
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

        /// <summary>
        /// Runtime validation : vérifier que l'Animator est correctement configuré.
        /// Retourne true si validation réussie, false et log détaillé sinon.
        /// Permet d'identifier les causes T-pose : controller absent, state par défaut non-configuré, clips cassés, etc.
        /// </summary>
        public static bool ValidateAnimatorSetup(Animator? anim, string? entityName = null)
        {
            if (anim == null)
            {
                Debug.LogError($"[AnimationController] {entityName ?? "Entity"}: Animator is null — cannot validate.");
                return false;
            }

            var controller = anim.runtimeAnimatorController;
            if (controller == null)
            {
                Debug.LogWarning($"[AnimationController] {entityName ?? "Entity"}: runtimeAnimatorController is null — entity may render T-pose. Build Animator Controllers if missing.");
                return false;
            }

            var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.fullPathHash == 0)
            {
                Debug.LogError($"[AnimationController] {entityName ?? "Entity"}: current state hash is 0 — animator not initialized or no default state set.");
                return false;
            }

            // Log the current state for diagnostics
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[AnimationController] {entityName ?? "Entity"} OK: state='{stateInfo.shortNameHash}' controller='{controller.name}'");
#endif
            return true;
        }

    }
}
