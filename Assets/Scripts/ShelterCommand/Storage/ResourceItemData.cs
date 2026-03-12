using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// ScriptableObject defining a physical resource item.
    /// Create via Assets > Create > ShelterCommand > ResourceItemData.
    /// </summary>
    [CreateAssetMenu(menuName = "ShelterCommand/ResourceItemData", fileName = "ResourceItemData")]
    public class ResourceItemData : ScriptableObject
    {
        [Tooltip("Unique identifier used to match items to resource types.")]
        public ResourceType resourceType;

        [Tooltip("Display name shown in the interaction prompt.")]
        public string displayName = "Ressource";

        [Tooltip("Color applied to the item's MeshRenderer material (for visual distinction).")]
        public Color itemColor = Color.white;
    }
}
