using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Single spatial shell. Looks are variants, not four houses the visitor picks.
    /// </summary>
    public class DojoShell : MonoBehaviour
    {
        public ProfileLook[] looks = new ProfileLook[Profiles.Count];

        public ProfileLook GetLook(ProfileId id)
        {
            var i = (int)id;
            if (looks == null || i < 0 || i >= looks.Length)
                return null;
            return looks[i];
        }

        public void HideAllLooksImmediate()
        {
            if (looks == null)
                return;
            foreach (var look in looks)
            {
                if (look != null && look.transition != null)
                    look.transition.HideImmediate();
            }
        }
    }
}
