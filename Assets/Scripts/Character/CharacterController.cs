using System;
using System.Collections.Generic;
using UnityEngine;

namespace iDate.Character
{
    /// <summary>
    /// Equipment slot types for character customization
    /// </summary>
    public enum EquipSlot
    {
        Head,       // Hair, hats, helmets
        Face,       // Facial features
        Upper,      // Shirts, jackets
        Lower,      // Pants, skirts
        Foot,       // Shoes, boots
        Hand,       // Gloves, bracelets
        Back,       // Backpacks, wings
        Neck,       // Necklaces
        Accessory1, // Extra slot 1
        Accessory2  // Extra slot 2
    }

    /// <summary>
    /// Character data structure
    /// </summary>
    [Serializable]
    public class CharacterData
    {
        public string CharacterId;
        public string CharacterName;
        public int Gender; // 0 = Male, 1 = Female
        public int BaseModel; // M01, F01, etc.
        
        // Equipment
        public Dictionary<EquipSlot, int> EquippedItems = new Dictionary<EquipSlot, int>();
        
        // Customization
        public Color SkinColor = Color.white;
        public Color HairColor = Color.black;
        
        // Stats (for future use)
        public int Level = 1;
        public int Experience = 0;
    }

    /// <summary>
    /// Character controller - manages character model, equipment, and animations
    /// </summary>
    public class CharacterController : MonoBehaviour
    {
        [Header("Character Data")]
        public CharacterData Data;
        
        [Header("Model Parts")]
        private Dictionary<EquipSlot, GameObject> _loadedParts = new Dictionary<EquipSlot, GameObject>();
        private SkinnedMeshRenderer[] _meshRenderers;
        private Transform _rootBone;
        
        [Header("Animation")]
        private Animator _animator;
        private string _currentAnimation = "idle";
        
        [Header("Settings")]
        public bool AutoLoadOnStart = true;
        public string DefaultModelPath = "Media/Charactor/Skin/";

        private void Start()
        {
            if (AutoLoadOnStart && Data != null)
            {
                LoadCharacter();
            }
        }

        /// <summary>
        /// Load the complete character model
        /// </summary>
        public void LoadCharacter()
        {
            if (Data == null)
            {
                Debug.LogError("CharacterController: No character data assigned!");
                return;
            }

            Debug.Log($"Loading character: {Data.CharacterName}");
            
            // Clear existing model
            ClearModel();
            
            // Load base skeleton
            LoadBaseSkeleton();
            
            // Load equipped parts
            foreach (var kvp in Data.EquippedItems)
            {
                LoadEquipment(kvp.Key, kvp.Value);
            }
            
            // Apply customization
            ApplyCustomization();
            
            // Setup animator
            SetupAnimator();
            
            Debug.Log($"Character loaded successfully: {Data.CharacterName}");
        }

        /// <summary>
        /// Load the base skeleton structure
        /// </summary>
        private void LoadBaseSkeleton()
        {
            // For now, create a basic skeleton structure
            // In full implementation, this would load from GB3
            
            GameObject skeleton = new GameObject("Skeleton");
            skeleton.transform.SetParent(transform, false);
            
            // Create basic Bip01 structure
            _rootBone = CreateBone("Bip01", skeleton.transform);
            Transform pelvis = CreateBone("Bip01 Pelvis", _rootBone);
            Transform spine = CreateBone("Bip01 Spine", pelvis);
            Transform spine1 = CreateBone("Bip01 Spine1", spine);
            Transform spine2 = CreateBone("Bip01 Spine2", spine1);
            Transform neck = CreateBone("Bip01 Neck", spine2);
            Transform head = CreateBone("Bip01 Head", neck);
            
            // Arms
            Transform lClavicle = CreateBone("Bip01 L Clavicle", spine2);
            Transform lUpperArm = CreateBone("Bip01 L UpperArm", lClavicle);
            Transform lForearm = CreateBone("Bip01 L Forearm", lUpperArm);
            Transform lHand = CreateBone("Bip01 L Hand", lForearm);
            
            Transform rClavicle = CreateBone("Bip01 R Clavicle", spine2);
            Transform rUpperArm = CreateBone("Bip01 R UpperArm", rClavicle);
            Transform rForearm = CreateBone("Bip01 R Forearm", rUpperArm);
            Transform rHand = CreateBone("Bip01 R Hand", rForearm);
            
            // Legs
            Transform lThigh = CreateBone("Bip01 L Thigh", pelvis);
            Transform lCalf = CreateBone("Bip01 L Calf", lThigh);
            Transform lFoot = CreateBone("Bip01 L Foot", lCalf);
            
            Transform rThigh = CreateBone("Bip01 R Thigh", pelvis);
            Transform rCalf = CreateBone("Bip01 R Calf", rThigh);
            Transform rFoot = CreateBone("Bip01 R Foot", rCalf);
            
            Debug.Log("Base skeleton created");
        }

        private Transform CreateBone(string name, Transform parent)
        {
            GameObject bone = new GameObject(name);
            bone.transform.SetParent(parent, false);
            return bone.transform;
        }

        /// <summary>
        /// Load equipment for a specific slot
        /// </summary>
        public void LoadEquipment(EquipSlot slot, int itemId)
        {
            Debug.Log($"Loading equipment: {slot} = {itemId}");
            
            // Unload existing item in slot
            UnloadEquipment(slot);
            
            // Build file path based on slot and item ID
            string filePath = BuildEquipmentPath(slot, itemId);
            
            // Load GB3 model
            GameObject part = LoadGB3Model(filePath);
            
            if (part != null)
            {
                part.transform.SetParent(transform, false);
                _loadedParts[slot] = part;
                
                // Attach to skeleton
                AttachToSkeleton(part);
            }
        }

        /// <summary>
        /// Unload equipment from a slot
        /// </summary>
        public void UnloadEquipment(EquipSlot slot)
        {
            if (_loadedParts.ContainsKey(slot))
            {
                Destroy(_loadedParts[slot]);
                _loadedParts.Remove(slot);
            }
        }

        /// <summary>
        /// Build file path for equipment
        /// </summary>
        private string BuildEquipmentPath(EquipSlot slot, int itemId)
        {
            string gender = Data.Gender == 0 ? "M" : "F";
            string model = $"{gender}01"; // M01 or F01
            
            string slotCode = slot switch
            {
                EquipSlot.Head => "CR_HEAD",
                EquipSlot.Face => "PA_FACE",
                EquipSlot.Upper => "CR_UPER",
                EquipSlot.Lower => "CR_LOWR",
                EquipSlot.Foot => "CR_FOOT",
                EquipSlot.Hand => "AC_HAND",
                EquipSlot.Back => "AC_BAG1",
                EquipSlot.Neck => "AC_NECK",
                _ => "PA_FACE"
            };
            
            // Format: F01_CR_HEAD_0004_00.gb3
            string filename = $"{model}_{slotCode}_{itemId:D4}_00.gb3";
            string fullPath = System.IO.Path.Combine(
                Application.dataPath,
                "../GameClient",
                DefaultModelPath,
                filename
            );
            
            return fullPath;
        }

        /// <summary>
        /// Load GB3 model file
        /// </summary>
        private GameObject LoadGB3Model(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogWarning($"GB3 file not found: {filePath}");
                // Create placeholder
                return CreatePlaceholder();
            }

            try
            {
                // Use GB3Loader to load the model
                var gb3Model = iDate.Assets.GB3Loader.Load(filePath);
                if (gb3Model != null)
                {
                    return iDate.Assets.GB3Loader.ToGameObject(gb3Model);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load GB3: {ex.Message}");
            }
            
            return CreatePlaceholder();
        }

        /// <summary>
        /// Create placeholder geometry for missing models
        /// </summary>
        private GameObject CreatePlaceholder()
        {
            GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholder.name = "Placeholder";
            placeholder.transform.localScale = Vector3.one * 0.2f;
            
            // Add a colored material
            var renderer = placeholder.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(
                    UnityEngine.Random.value,
                    UnityEngine.Random.value,
                    UnityEngine.Random.value
                );
            }
            
            return placeholder;
        }

        /// <summary>
        /// Attach loaded part to skeleton
        /// </summary>
        private void AttachToSkeleton(GameObject part)
        {
            // Find SkinnedMeshRenderer in the part
            SkinnedMeshRenderer smr = part.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && _rootBone != null)
            {
                // Map bones from part to our skeleton
                // This is a simplified version - full implementation would match by name
                smr.rootBone = _rootBone;
            }
        }

        /// <summary>
        /// Apply color customization
        /// </summary>
        private void ApplyCustomization()
        {
            // Apply skin color, hair color, etc.
            foreach (var part in _loadedParts.Values)
            {
                var renderers = part.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    // Apply tint based on part type
                    // This is simplified - real implementation would use material properties
                    if (renderer.material.HasProperty("_Color"))
                    {
                        renderer.material.color = Data.SkinColor;
                    }
                }
            }
        }

        /// <summary>
        /// Setup animator component
        /// </summary>
        private void SetupAnimator()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = gameObject.AddComponent<Animator>();
            }
            
            // Load animator controller (to be implemented)
            // _animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Animations/CharacterController");
            
            PlayAnimation("idle");
        }

        /// <summary>
        /// Play animation by name
        /// </summary>
        public void PlayAnimation(string animationName)
        {
            _currentAnimation = animationName;
            
            if (_animator != null)
            {
                _animator.Play(animationName);
            }
            
            Debug.Log($"Playing animation: {animationName}");
        }

        /// <summary>
        /// Clear all loaded model parts
        /// </summary>
        private void ClearModel()
        {
            foreach (var part in _loadedParts.Values)
            {
                if (part != null)
                {
                    Destroy(part);
                }
            }
            _loadedParts.Clear();
            
            // Clear skeleton
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Get current animation name
        /// </summary>
        public string GetCurrentAnimation()
        {
            return _currentAnimation;
        }

        /// <summary>
        /// Check if equipment slot is occupied
        /// </summary>
        public bool HasEquipment(EquipSlot slot)
        {
            return _loadedParts.ContainsKey(slot);
        }

        private void OnDestroy()
        {
            ClearModel();
        }

        // Editor helper
        [ContextMenu("Reload Character")]
        public void ReloadCharacter()
        {
            LoadCharacter();
        }

        [ContextMenu("Create Test Character")]
        public void CreateTestCharacter()
        {
            Data = new CharacterData
            {
                CharacterId = "TEST001",
                CharacterName = "Test Character",
                Gender = 1, // Female
                BaseModel = 1
            };
            
            // Equip some default items
            Data.EquippedItems[EquipSlot.Face] = 1;
            Data.EquippedItems[EquipSlot.Head] = 4;
            Data.EquippedItems[EquipSlot.Upper] = 4;
            Data.EquippedItems[EquipSlot.Lower] = 4;
            Data.EquippedItems[EquipSlot.Foot] = 4;
            
            LoadCharacter();
        }
    }
}
