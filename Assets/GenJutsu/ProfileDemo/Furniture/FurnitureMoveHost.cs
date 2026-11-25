using GenJutsu.GenCore;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Binds the active look, parses demo commands, and drives FurnitureMover.
    /// </summary>
    public sealed class FurnitureMoveHost : MonoBehaviour
    {
        public ProfileDemoDirector director;
        FurnitureMover mover;
        string houseKey;
        Transform boundHouse;

        public static FurnitureMoveHost Ensure(ProfileDemoDirector demo)
        {
            if (demo == null)
                return null;
            if (!demo.TryGetComponent<FurnitureMoveHost>(out var host))
                host = demo.gameObject.AddComponent<FurnitureMoveHost>();
            host.director = demo;
            return host;
        }

        public bool TryHandle(string speech, out string reply)
        {
            reply = "";
            BindActive();
            if (string.IsNullOrWhiteSpace(houseKey))
                return false;
            var intent = FurnitureCommandParser.Parse(speech, houseKey);
            if (!intent.Matched)
                return false;
            if (mover == null)
            {
                reply = "A casa ainda não está pronta.";
                return true;
            }

            return mover.TryRun(intent, out reply);
        }

        public FurnitureHouseCatalog ActiveCatalog()
        {
            BindActive();
            return FurnitureMoveCatalog.ForHouse(houseKey);
        }

        public void BindActive()
        {
            if (director == null)
                director = GetComponent<ProfileDemoDirector>();
            if (director == null || director.Current != ProfileDemoDirector.State.InHouse)
            {
                houseKey = "";
                return;
            }

            var look = ActiveLook();
            if (look == null)
                return;
            var root = look.transform;
            houseKey = director.Active.ToString();
            if (boundHouse == root && mover != null)
                return;
            boundHouse = root;
            mover = FurnitureLayout.Ensure(root, houseKey);
        }

        Transform ActiveLook()
        {
            if (director.shell != null)
            {
                var look = director.shell.GetLook(director.Active);
                if (look != null)
                    return look.transform;
            }

            var i = (int)director.Active;
            if (director.houses != null && i >= 0 && i < director.houses.Length && director.houses[i] != null)
                return director.houses[i].transform;
            return null;
        }

        void LateUpdate()
        {
            if (director != null && director.Current == ProfileDemoDirector.State.InHouse)
                BindActive();
        }
    }
}
