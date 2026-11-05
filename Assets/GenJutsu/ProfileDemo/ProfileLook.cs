using UnityEngine;
using UnityEngine.Rendering;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// One look (Z/C/E/A) parented to the shared DojoShell — not a separate destination building.
    /// </summary>
    public class ProfileLook : MonoBehaviour
    {
        public ProfileId profile;
        public HouseTransition transition;
        public Volume volume;
    }
}
