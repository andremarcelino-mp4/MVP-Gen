using UnityEngine;

namespace GenJutsu.GenCore
{
    public readonly struct FurnitureSpec
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string[] Synonyms;
        public readonly string[] HierarchyNames;

        public FurnitureSpec(string id, string displayName, string[] synonyms, string[] hierarchyNames)
        {
            Id = id;
            DisplayName = displayName;
            Synonyms = synonyms;
            HierarchyNames = hierarchyNames;
        }
    }

    public readonly struct FurnitureAnchorSpec
    {
        public readonly string Id;
        public readonly string[] Synonyms;
        public readonly Vector3 LocalPosition;
        public readonly float Yaw;

        public FurnitureAnchorSpec(string id, string[] synonyms, Vector3 localPosition, float yaw)
        {
            Id = id;
            Synonyms = synonyms;
            LocalPosition = localPosition;
            Yaw = yaw;
        }
    }

    public readonly struct FurnitureDemoCommand
    {
        public readonly string Phrase;
        public readonly string FurnitureId;
        public readonly string TargetId;
        public readonly bool Reset;

        public FurnitureDemoCommand(string phrase, string furnitureId, string targetId, bool reset)
        {
            Phrase = phrase;
            FurnitureId = furnitureId;
            TargetId = targetId;
            Reset = reset;
        }
    }

    public sealed class FurnitureHouseCatalog
    {
        public readonly string HouseKey;
        public readonly FurnitureSpec[] Furniture;
        public readonly FurnitureAnchorSpec[] Anchors;
        public readonly FurnitureDemoCommand[] Commands;

        public FurnitureHouseCatalog(string houseKey, FurnitureSpec[] furniture, FurnitureAnchorSpec[] anchors,
            FurnitureDemoCommand[] commands)
        {
            HouseKey = houseKey;
            Furniture = furniture;
            Anchors = anchors;
            Commands = commands;
        }
    }

    /// <summary>
    /// Demo-safe movable pieces and named floor spots for each ZCEA look.
    /// Positions sit inside the shared dojo (~5 m interior, posts at ±2.52).
    /// </summary>
    public static class FurnitureMoveCatalog
    {
        public static readonly FurnitureHouseCatalog[] Houses =
        {
            Zen(),
            Cyber(),
            Excentrico(),
            Acolhedor()
        };

        public static FurnitureHouseCatalog ForHouse(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            for (var i = 0; i < Houses.Length; i++)
            {
                if (string.Equals(Houses[i].HouseKey, key, System.StringComparison.OrdinalIgnoreCase))
                    return Houses[i];
            }

            return null;
        }

        static FurnitureAnchorSpec[] SharedAnchors()
        {
            return new[]
            {
                new FurnitureAnchorSpec("janela", new[] { "janela", "janelinhas", "vidro", "shoji" },
                    new Vector3(0f, 0f, 1.72f), 180f),
                new FurnitureAnchorSpec("centro", new[] { "centro", "meio", "central" },
                    new Vector3(0f, 0f, 0.08f), 0f),
                new FurnitureAnchorSpec("parede", new[] { "parede", "lateral" },
                    new Vector3(-1.55f, 0f, 0.2f), 90f),
                new FurnitureAnchorSpec("canto", new[] { "canto", "esquina" },
                    new Vector3(1.48f, 0f, -1.42f), -35f)
            };
        }

        static FurnitureDemoCommand ResetCmd()
        {
            return new FurnitureDemoCommand("Gen, volta tudo pro lugar", "", "", true);
        }

        static FurnitureHouseCatalog Zen()
        {
            return new FurnitureHouseCatalog("Zen",
                new[]
                {
                    new FurnitureSpec("mesa", "mesa",
                        new[] { "mesa", "mesinha", "chabudai" },
                        new[] { "LowTable", "Chabudai", "Table" }),
                    new FurnitureSpec("luminaria", "luminária",
                        new[] { "luminaria", "lanterna", "abajur", "lampada" },
                        new[] { "Lanterns", "Lantern", "PaperLantern" }),
                    new FurnitureSpec("bonsai", "bonsai",
                        new[] { "bonsai", "arvorezinha", "arvore" },
                        new[] { "Bonsai" }),
                    new FurnitureSpec("jardim", "jardim de pedras",
                        new[] { "jardim", "pedras", "rochas" },
                        new[] { "RockGarden" })
                },
                SharedAnchors(),
                new[]
                {
                    new FurnitureDemoCommand("Gen, traz a mesa pro centro", "mesa", "centro", false),
                    new FurnitureDemoCommand("Gen, leva a luminária pra perto da janela", "luminaria", "janela", false),
                    new FurnitureDemoCommand("Gen, move o bonsai pro canto", "bonsai", "canto", false),
                    ResetCmd()
                });
        }

        static FurnitureHouseCatalog Cyber()
        {
            return new FurnitureHouseCatalog("Cyber",
                new[]
                {
                    new FurnitureSpec("mesa", "mesa",
                        new[] { "mesa", "escrivaninha", "desk" },
                        new[] { "LowTable", "Desk", "Table" }),
                    new FurnitureSpec("holo", "holograma",
                        new[] { "holograma", "holo", "painel", "tela" },
                        new[] { "Holo", "Pane" }),
                    new FurnitureSpec("cubo", "cubo",
                        new[] { "cubo", "node", "bloco" },
                        new[] { "Node" }),
                    new FurnitureSpec("luminaria", "luminária",
                        new[] { "luminaria", "lanterna", "neon" },
                        new[] { "Lanterns", "Lantern" })
                },
                SharedAnchors(),
                new[]
                {
                    new FurnitureDemoCommand("Gen, move a mesa pro centro", "mesa", "centro", false),
                    new FurnitureDemoCommand("Gen, traz o holograma pra parede", "holo", "parede", false),
                    new FurnitureDemoCommand("Gen, leva o cubo pro canto", "cubo", "canto", false),
                    ResetCmd()
                });
        }

        static FurnitureHouseCatalog Excentrico()
        {
            return new FurnitureHouseCatalog("Excentrico",
                new[]
                {
                    new FurnitureSpec("mesa", "mesa",
                        new[] { "mesa", "mesinha" },
                        new[] { "LowTable", "Table" }),
                    new FurnitureSpec("quadro", "quadro",
                        new[] { "quadro", "pintura", "tela" },
                        new[] { "Painting", "Frame" }),
                    new FurnitureSpec("objeto", "objeto",
                        new[] { "objeto", "forma", "estrela" },
                        new[] { "ShapeA", "ShapeB", "ShapeC" }),
                    new FurnitureSpec("luminaria", "luminária",
                        new[] { "luminaria", "lanterna" },
                        new[] { "Lanterns", "Lantern" })
                },
                SharedAnchors(),
                new[]
                {
                    new FurnitureDemoCommand("Gen, traz a mesa pra perto da janela", "mesa", "janela", false),
                    new FurnitureDemoCommand("Gen, move o quadro pro centro", "quadro", "centro", false),
                    new FurnitureDemoCommand("Gen, leva o objeto pro canto", "objeto", "canto", false),
                    ResetCmd()
                });
        }

        static FurnitureHouseCatalog Acolhedor()
        {
            return new FurnitureHouseCatalog("Acolhedor",
                new[]
                {
                    new FurnitureSpec("mesa", "mesa",
                        new[] { "mesa", "mesinha", "kotatsu" },
                        new[] { "LowTable", "Kotatsu", "Table" }),
                    new FurnitureSpec("almofada", "almofada",
                        new[] { "almofada", "poltrona", "sofa", "sofá", "futon", "cushion" },
                        new[] { "Cushion", "CushionB", "Sofa" }),
                    new FurnitureSpec("planta", "planta",
                        new[] { "planta", "vaso", "folha" },
                        new[] { "Plant" }),
                    new FurnitureSpec("luminaria", "luminária",
                        new[] { "luminaria", "lanterna", "abajur" },
                        new[] { "Lanterns", "Lantern" })
                },
                SharedAnchors(),
                new[]
                {
                    new FurnitureDemoCommand("Gen, traz o sofá pra perto da janela", "almofada", "janela", false),
                    new FurnitureDemoCommand("Gen, move a mesa pro centro", "mesa", "centro", false),
                    new FurnitureDemoCommand("Gen, leva a planta pra parede", "planta", "parede", false),
                    ResetCmd()
                });
        }
    }
}
