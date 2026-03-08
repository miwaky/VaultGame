using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.ProBuilder;

#if UNITY_EDITOR
public class LevelGeneratorWindow : EditorWindow
{
    private Material wallMaterial;
    private Material floorMaterial;
    private Material ceilingMaterial;
    private Material stairsMaterial;
    private Vector2 scrollPosition;
    
    private int selectedTab = 0;
    private readonly string[] tabNames = { "🗺️ Grille", "📖 Manuel" };
    
    private List<GridLayoutData> floors = new List<GridLayoutData>();
    private int currentFloor = 0;
    private float floorHeight = 3.5f;
    
    private int gridWidth = 16;
    private int gridHeight = 16;
    private float gridCellSize = 5f;
    private int gridMode = 0;
    private readonly string[] gridModeNames = { "● Placer Pièces", "🚪 Placer Portes", "🪜 Placer Escaliers", "✏️ Éditer" };
    private Vector2 gridScrollPosition;
    private string selectedRoomId = null;
    private Vector2Int? dragStartCell = null;
    
    private GridStairs.Direction currentStairsDirection = GridStairs.Direction.North;
    
    private bool globalGenerateFloor = true;
    private bool globalGenerateCeiling = true;
    
    private LevelLayoutPreset currentPreset = null;
    
    private bool showGridPreview = false;
    private GameObject lastGeneratedLevel = null;
    
    private GridLayoutData gridLayout
    {
        get
        {
            if (floors.Count == 0)
            {
                floors.Add(new GridLayoutData(gridWidth, gridHeight));
            }
            if (currentFloor >= floors.Count)
            {
                currentFloor = floors.Count - 1;
            }
            return floors[currentFloor];
        }
    }

    [MenuItem("Tools/Level Generator")]
    public static void ShowWindow()
    {
        GetWindow<LevelGeneratorWindow>("Level Generator");
    }
    
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        if (floors.Count == 0)
        {
            floors.Add(new GridLayoutData(gridWidth, gridHeight));
        }
    }
    
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    
    private void OnSceneGUI(SceneView sceneView)
    {
        if (showGridPreview && gridLayout != null && gridLayout.rooms.Count > 0)
        {
            DrawGridPreviewInScene();
        }
        
        sceneView.Repaint();
    }

    private void OnGUI()
    {
        GUILayout.Label("🎨 Générateur de Niveau - Grille", EditorStyles.boldLabel);
        GUILayout.Space(5);

        selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30));
        GUILayout.Space(10);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        if (selectedTab == 0)
        {
            DrawGridTab();
        }
        else if (selectedTab == 1)
        {
            DrawManuelTab();
        }
        
        EditorGUILayout.EndScrollView();
    }
   
 
    private void DrawGridTab()
    {
        EditorGUILayout.HelpBox(
            "🎨 GRILLE VISUELLE - Éditeur de Niveau\n\n" +
            "Créez votre niveau en dessinant directement sur la grille !\n\n" +
            "• Mode 1 : Placer des pièces (cliquez-glissez)\n" +
            "• Mode 2 : Placer des portes entre pièces\n" +
            "• Mode 3 : Éditer (ajout/suppression de cellules)\n\n" +
            "💡 Utilisez le bouton Prévisualiser pour voir le résultat en 3D dans la Scene View !",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        EditorGUILayout.LabelField("💾 Sauvegarde / Chargement", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Sauvegardez et chargez vos configurations de niveaux pour les réutiliser", MessageType.Info);
        
        EditorGUILayout.BeginHorizontal();
        currentPreset = (LevelLayoutPreset)EditorGUILayout.ObjectField("Preset actuel", currentPreset, typeof(LevelLayoutPreset), false);
        EditorGUILayout.EndHorizontal();
        
        if (currentPreset != null)
        {
            EditorGUILayout.HelpBox(
                $"📋 {currentPreset.metadata.presetName}\n" +
                $"Étages: {currentPreset.floors.Count} | " +
                $"Grille: {currentPreset.gridWidth}×{currentPreset.gridHeight} | " +
                $"Modifié: {currentPreset.metadata.lastModifiedDate}",
                MessageType.None
            );
        }
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("💾 Sauvegarder", GUILayout.Height(30)))
        {
            SaveCurrentLayout();
        }
        
        GUI.enabled = currentPreset != null;
        if (GUILayout.Button("📂 Charger", GUILayout.Height(30)))
        {
            LoadLayoutFromPreset();
        }
        GUI.enabled = true;
        
        if (GUILayout.Button("🆕 Nouveau Preset", GUILayout.Height(30)))
        {
            CreateNewPreset();
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        EditorGUILayout.LabelField("🎨 Matériaux par Défaut", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Ces matériaux seront utilisés par défaut pour toutes les nouvelles pièces", MessageType.Info);
        wallMaterial = (Material)EditorGUILayout.ObjectField("Murs", wallMaterial, typeof(Material), false);
        floorMaterial = (Material)EditorGUILayout.ObjectField("Sol", floorMaterial, typeof(Material), false);
        ceilingMaterial = (Material)EditorGUILayout.ObjectField("Plafond", ceilingMaterial, typeof(Material), false);
        stairsMaterial = (Material)EditorGUILayout.ObjectField("🪜 Escaliers", stairsMaterial, typeof(Material), false);
        
        GUILayout.Space(10);
        
        EditorGUILayout.LabelField("🔧 Options Globales de Génération", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Ces options s'appliquent à toutes les pièces lors de la génération", MessageType.Info);
        
        EditorGUILayout.BeginHorizontal();
        globalGenerateFloor = EditorGUILayout.Toggle("Générer tous les sols", globalGenerateFloor);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("☑ Activer tout", GUILayout.Width(100)))
        {
            foreach (var floor in floors)
            {
                foreach (var room in floor.rooms)
                {
                    room.hasFloor = true;
                }
            }
        }
        if (GUILayout.Button("☐ Désactiver tout", GUILayout.Width(110)))
        {
            foreach (var floor in floors)
            {
                foreach (var room in floor.rooms)
                {
                    room.hasFloor = false;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        globalGenerateCeiling = EditorGUILayout.Toggle("Générer tous les plafonds", globalGenerateCeiling);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("☑ Activer tout", GUILayout.Width(100)))
        {
            foreach (var floor in floors)
            {
                foreach (var room in floor.rooms)
                {
                    room.hasCeiling = true;
                }
            }
        }
        if (GUILayout.Button("☐ Désactiver tout", GUILayout.Width(110)))
        {
            foreach (var floor in floors)
            {
                foreach (var room in floor.rooms)
                {
                    room.hasCeiling = false;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        EditorGUILayout.LabelField("🏢 Gestion des Étages", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Étage Actif :", GUILayout.Width(100));
        
        for (int i = 0; i < floors.Count; i++)
        {
            Color oldColor = GUI.backgroundColor;
            if (i == currentFloor)
            {
                GUI.backgroundColor = Color.green;
            }
            
            if (GUILayout.Button($"{i + 1}", GUILayout.Width(35), GUILayout.Height(25)))
            {
                currentFloor = i;
                selectedRoomId = null;
            }
            
            GUI.backgroundColor = oldColor;
        }
        
        if (GUILayout.Button("+", GUILayout.Width(35), GUILayout.Height(25)))
        {
            floors.Add(new GridLayoutData(gridWidth, gridHeight));
            currentFloor = floors.Count - 1;
            selectedRoomId = null;
        }
        
        if (floors.Count > 1 && GUILayout.Button("-", GUILayout.Width(35), GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Supprimer l'étage", 
                $"Voulez-vous vraiment supprimer l'étage {currentFloor + 1} ?", 
                "Oui", "Non"))
            {
                floors.RemoveAt(currentFloor);
                if (currentFloor >= floors.Count)
                {
                    currentFloor = floors.Count - 1;
                }
                selectedRoomId = null;
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"📊 Étage {currentFloor + 1}/{floors.Count}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"Hauteur : {currentFloor * floorHeight}m", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        
        floorHeight = EditorGUILayout.Slider("Hauteur par étage (m)", floorHeight, 2.5f, 6f);
        
        GUILayout.Space(10);
        
        EditorGUILayout.LabelField("⚙️ Configuration de la Grille", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        int newWidth = EditorGUILayout.IntSlider("Largeur", gridLayout.gridWidth, 4, 32);
        int newHeight = EditorGUILayout.IntSlider("Hauteur", gridLayout.gridHeight, 4, 32);
        if (EditorGUI.EndChangeCheck())
        {
            gridWidth = newWidth;
            gridHeight = newHeight;
            floors[currentFloor] = new GridLayoutData(gridWidth, gridHeight);
        }
        
        gridCellSize = EditorGUILayout.Slider("Taille cellule (mètres)", gridCellSize, 2f, 10f);
        gridLayout.cellSize = gridCellSize;
        
        GUILayout.Space(10);
        
        EditorGUILayout.LabelField("🎮 Mode d'Édition", EditorStyles.boldLabel);
        gridMode = GUILayout.SelectionGrid(gridMode, gridModeNames, 2, GUILayout.Height(50));
        
        if (gridMode == 2)
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Direction de l'escalier:", EditorStyles.miniLabel);
            string[] directionNames = { "↑ Nord", "↓ Sud", "→ Est", "← Ouest" };
            int directionIndex = (int)currentStairsDirection;
            directionIndex = GUILayout.SelectionGrid(directionIndex, directionNames, 4, GUILayout.Height(25));
            currentStairsDirection = (GridStairs.Direction)directionIndex;
        }
        
        GUILayout.Space(10);
        
        EditorGUILayout.LabelField("🗺️ Grille de Niveau", EditorStyles.boldLabel);
        
        DrawGrid();
        
        GUILayout.Space(10);
        
        if (gridLayout.rooms.Count > 0)
        {
            DrawRoomsList();
            
            if (gridLayout.doors.Count > 0)
            {
                GUILayout.Space(10);
                DrawDoorsList();
            }
            
            GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            Color originalBgColor = GUI.backgroundColor;
            if (showGridPreview)
            {
                GUI.backgroundColor = Color.green;
            }
            
            string previewButtonText = showGridPreview ? "✅ Preview Activé" : "🔍 Prévisualiser";
            if (GUILayout.Button(previewButtonText, GUILayout.Height(35)))
            {
                PreviewGridLayout();
            }
            
            GUI.backgroundColor = originalBgColor;
            if (GUILayout.Button("🏗️ Générer le Niveau", GUILayout.Height(35)))
            {
                GenerateFromGridLayout();
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🗑️ Effacer le plan", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Confirmation", "Effacer le plan actuel ?\n(Les niveaux générés dans la scène seront conservés)", "Oui", "Non"))
                {
                    floors[currentFloor] = new GridLayoutData(gridWidth, gridHeight);
                    selectedRoomId = null;
                }
            }
            
            GUI.enabled = lastGeneratedLevel != null;
            if (GUILayout.Button("↩️ Annuler le niveau", GUILayout.Height(25)))
            {
                if (lastGeneratedLevel != null)
                {
                    if (EditorUtility.DisplayDialog("Confirmation", $"Supprimer '{lastGeneratedLevel.name}' de la scène ?", "Oui", "Non"))
                    {
                        DestroyImmediate(lastGeneratedLevel);
                        lastGeneratedLevel = null;
                        Debug.Log("Dernier niveau supprimé");
                    }
                }
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Aucune pièce créée. Utilisez le mode 'Placer Pièces' et cliquez sur la grille !", MessageType.Warning);
        }
    }
    
    private void DrawManuelTab()
    {
        GUILayout.Label("📖 Manuel d'Utilisation", EditorStyles.largeLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Bienvenue dans le Level Generator !\n\n" +
            "Cet outil vous permet de créer des niveaux en dessinant directement sur une grille visuelle. " +
            "Vous pouvez placer des pièces, les relier avec des portes, et personnaliser chaque élément.",
            MessageType.Info
        );
        
        GUILayout.Space(15);
        
        DrawManuelSection("🎨 1. Configuration des Matériaux",
            "Avant de commencer, définissez les matériaux par défaut pour vos niveaux :\n\n" +
            "• Murs : Matériau pour les murs de toutes les pièces\n" +
            "• Sol : Matériau pour le sol\n" +
            "• Plafond : Matériau pour le plafond\n\n" +
            "💡 ASTUCE : Utilisez Tools > Create Default Level Materials pour créer des matériaux de base.",
            Color.cyan
        );
        
        GUILayout.Space(10);
        
        DrawManuelSection("⚙️ 2. Configuration de la Grille",
            "• Largeur / Hauteur : Taille de la grille (4 à 32 cellules)\n" +
            "• Taille cellule : Dimension d'une cellule en mètres (2m à 10m)\n\n" +
            "💡 Une cellule de 5m × 5m est idéale pour des pièces classiques.",
            Color.yellow
        );
        
        GUILayout.Space(10);
        
        DrawManuelSection("🎮 3. Les Trois Modes d'Édition",
            "MODE 1 - Placer Pièces :\n" +
            "• Cliquez sur une cellule vide pour créer une nouvelle pièce\n" +
            "• Maintenez et glissez pour agrandir la pièce\n" +
            "• Cliquez sur une pièce existante pour la sélectionner\n\n" +
            "MODE 2 - Placer Portes :\n" +
            "• Cliquez sur le bord entre deux pièces pour créer une porte\n" +
            "• Les portes connectent automatiquement les pièces adjacentes\n\n" +
            "MODE 3 - Éditer :\n" +
            "• Cliquez sur une cellule vide pour l'ajouter à la pièce sélectionnée\n" +
            "• Cliquez sur une cellule de la pièce sélectionnée pour la retirer\n" +
            "• Permet de créer des formes complexes (L, T, etc.)",
            Color.green
        );
        
        GUILayout.Space(10);
        
        DrawManuelSection("🔧 4. Configuration des Pièces",
            "Pour chaque pièce, vous pouvez personnaliser :\n\n" +
            "• Nom : Donnez un nom unique à votre pièce\n" +
            "• Couleur : Choisissez une couleur pour la grille\n\n" +
            "TAILLE PERSONNALISÉE :\n" +
            "• Activez pour ignorer la grille et définir une taille exacte en mètres\n" +
            "• Utile pour créer des couloirs étroits ou de grandes salles\n\n" +
            "HAUTEURS :\n" +
            "• Hauteur Sol : Position verticale du sol (pour créer des marches)\n" +
            "• Hauteur Plafond : Hauteur du plafond depuis le sol\n" +
            "💡 Utilisez différentes hauteurs pour créer du relief vertical\n\n" +
            "MATÉRIAUX PERSONNALISÉS :\n" +
            "• Vous pouvez assigner des matériaux différents par pièce\n" +
            "• Laissez vide pour utiliser les matériaux par défaut",
            Color.magenta
        );
        
        GUILayout.Space(10);
        
        DrawManuelSection("👁️ 5. Prévisualisation et Génération",
            "PRÉVISUALISER :\n" +
            "• Affiche un aperçu 3D coloré dans la Scene View\n" +
            "• Permet de vérifier le layout avant génération\n" +
            "• Les couleurs correspondent à celles de la grille\n\n" +
            "GÉNÉRER LE NIVEAU :\n" +
            "• Crée les pièces 3D avec ProBuilder\n" +
            "• Les portes sont automatiquement placées\n" +
            "• Un objet parent 'GridLevel' regroupe tout le niveau\n\n" +
            "EFFACER LE PLAN :\n" +
            "• Efface uniquement le plan sur la grille\n" +
            "• Les niveaux générés dans la scène restent intacts\n\n" +
            "EFFACER LE NIVEAU :\n" +
            "• Supprime le dernier niveau généré de la scène\n" +
            "• Le plan sur la grille est conservé",
            Color.blue
        );
        
        GUILayout.Space(10);
        
        DrawManuelSection("💡 6. Conseils et Astuces",
            "WORKFLOW RECOMMANDÉ :\n" +
            "1. Définissez vos matériaux par défaut\n" +
            "2. Créez le layout général avec le Mode 1 (Placer Pièces)\n" +
            "3. Ajoutez les portes avec le Mode 2 (Placer Portes)\n" +
            "4. Affinez avec le Mode 3 (Éditer) pour les formes complexes\n" +
            "5. Configurez chaque pièce (hauteurs, tailles, matériaux)\n" +
            "6. Prévisualisez pour vérifier\n" +
            "7. Générez le niveau final\n\n" +
            "ASTUCES :\n" +
            "• Utilisez Ctrl+Z pour annuler (fonctionne sur la grille)\n" +
            "• Les pièces adjacentes partagent automatiquement les murs\n" +
            "• Créez des couloirs en utilisant la taille personnalisée (ex: 2m × 10m)\n" +
            "• Variez les hauteurs de plafond pour plus de réalisme\n" +
            "• Utilisez des matériaux différents pour distinguer les zones\n\n" +
            "LIMITATIONS :\n" +
            "• Une porte ne peut relier que deux pièces adjacentes\n" +
            "• Les pièces doivent avoir au moins une cellule\n" +
            "• Les formes complexes nécessitent le Mode 3 (Éditer)",
            new Color(1f, 0.5f, 0f)
        );
        
        GUILayout.Space(15);
        
        EditorGUILayout.HelpBox(
            "🎉 Vous êtes maintenant prêt à créer vos premiers niveaux !\n\n" +
            "Retournez à l'onglet Grille pour commencer.",
            MessageType.None
        );
    }
    
    private void DrawManuelSection(string title, string content, Color color)
    {
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = color * 0.3f;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = originalColor;
        
        GUILayout.Label(title, EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        EditorGUILayout.LabelField(content, EditorStyles.wordWrappedLabel);
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawGrid()
    {
        float cellDisplaySize = 30f;
        int currentGridWidth = gridLayout.gridWidth;
        int currentGridHeight = gridLayout.gridHeight;
        float gridDisplayWidth = currentGridWidth * cellDisplaySize;
        float gridDisplayHeight = currentGridHeight * cellDisplaySize;
        
        Rect gridRect = GUILayoutUtility.GetRect(gridDisplayWidth, gridDisplayHeight);
        
        Event e = Event.current;
        Vector2Int? hoveredCell = GetCellFromMousePosition(e.mousePosition, gridRect, cellDisplaySize);
        
        for (int y = 0; y < currentGridHeight; y++)
        {
            for (int x = 0; x < currentGridWidth; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Rect cellRect = new Rect(
                    gridRect.x + x * cellDisplaySize,
                    gridRect.y + y * cellDisplaySize,
                    cellDisplaySize,
                    cellDisplaySize
                );
                
                string roomId = gridLayout.GetRoomAtCell(cell);
                Color cellColor = Color.white;
                
                if (!string.IsNullOrEmpty(roomId))
                {
                    GridRoom room = gridLayout.FindRoom(roomId);
                    if (room != null)
                    {
                        cellColor = room.previewColor;
                    }
                }
                
                if (hoveredCell.HasValue && hoveredCell.Value == cell)
                {
                    cellColor = Color.Lerp(cellColor, Color.yellow, 0.3f);
                }
                
                if (selectedRoomId != null && roomId == selectedRoomId)
                {
                    cellColor = Color.Lerp(cellColor, Color.green, 0.2f);
                }
                
                EditorGUI.DrawRect(cellRect, cellColor);
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1), Color.gray);
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1, cellRect.height), Color.gray);
                
                if (!string.IsNullOrEmpty(roomId))
                {
                    GUI.Label(cellRect, roomId, EditorStyles.centeredGreyMiniLabel);
                }
            }
        }
        
        DrawDoorsOnGrid(gridRect, cellDisplaySize);
        DrawStairsOnGrid(gridRect, cellDisplaySize);
        
        HandleGridInput(e, hoveredCell, gridRect);
    }
    
    private void DrawDoorsOnGrid(Rect gridRect, float cellDisplaySize)
    {
        GUIStyle doorStyle = new GUIStyle(EditorStyles.boldLabel);
        doorStyle.fontSize = 18;
        doorStyle.alignment = TextAnchor.MiddleCenter;
        doorStyle.normal.textColor = Color.red;
        
        foreach (var door in gridLayout.doors)
        {
            float x1 = gridRect.x + door.cell1.x * cellDisplaySize;
            float y1 = gridRect.y + door.cell1.y * cellDisplaySize;
            float x2 = gridRect.x + door.cell2.x * cellDisplaySize;
            float y2 = gridRect.y + door.cell2.y * cellDisplaySize;
            
            float centerX = (x1 + x2) * 0.5f + cellDisplaySize * 0.5f;
            float centerY = (y1 + y2) * 0.5f + cellDisplaySize * 0.5f;
            
            Rect doorRect = new Rect(centerX - 10, centerY - 10, 20, 20);
            GUI.Label(doorRect, "🚪", doorStyle);
        }
    }
    
    private void DrawStairsOnGrid(Rect gridRect, float cellDisplaySize)
    {
        GUIStyle stairsStyle = new GUIStyle(EditorStyles.boldLabel);
        stairsStyle.fontSize = 18;
        stairsStyle.alignment = TextAnchor.MiddleCenter;
        stairsStyle.normal.textColor = new Color(1f, 0.5f, 0f);
        
        foreach (var stairs in gridLayout.stairs)
        {
            float x = gridRect.x + stairs.cell.x * cellDisplaySize + cellDisplaySize * 0.5f;
            float y = gridRect.y + stairs.cell.y * cellDisplaySize + cellDisplaySize * 0.5f;
            
            Rect stairsRect = new Rect(x - 10, y - 10, 20, 20);
            GUI.Label(stairsRect, "🪜", stairsStyle);
            
            string directionArrow = "";
            switch (stairs.direction)
            {
                case GridStairs.Direction.North: directionArrow = "↑"; break;
                case GridStairs.Direction.South: directionArrow = "↓"; break;
                case GridStairs.Direction.East: directionArrow = "→"; break;
                case GridStairs.Direction.West: directionArrow = "←"; break;
            }
            
            GUIStyle arrowStyle = new GUIStyle(EditorStyles.miniLabel);
            arrowStyle.fontSize = 10;
            arrowStyle.alignment = TextAnchor.LowerCenter;
            arrowStyle.normal.textColor = Color.white;
            
            Rect arrowRect = new Rect(x - 10, y + 5, 20, 15);
            GUI.Label(arrowRect, directionArrow, arrowStyle);
        }
    }
    
    private Vector2Int? GetCellFromMousePosition(Vector2 mousePos, Rect gridRect, float cellSize)
    {
        if (!gridRect.Contains(mousePos))
            return null;
        
        int x = Mathf.FloorToInt((mousePos.x - gridRect.x) / cellSize);
        int y = Mathf.FloorToInt((mousePos.y - gridRect.y) / cellSize);
        
        if (x >= 0 && x < gridLayout.gridWidth && y >= 0 && y < gridLayout.gridHeight)
        {
            return new Vector2Int(x, y);
        }
        return null;
    }
    
    private void HandleGridInput(Event e, Vector2Int? hoveredCell, Rect gridRect)
    {
        if (!hoveredCell.HasValue || !gridRect.Contains(e.mousePosition))
            return;
        
        Vector2Int cell = hoveredCell.Value;
        
        if (gridMode == 0)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                string roomId = gridLayout.GetRoomAtCell(cell);
                if (string.IsNullOrEmpty(roomId))
                {
                    GridRoom newRoom = gridLayout.GetOrCreateRoom(cell);
                    selectedRoomId = newRoom.id;
                    dragStartCell = cell;
                }
                else
                {
                    selectedRoomId = roomId;
                }
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && dragStartCell.HasValue)
            {
                if (selectedRoomId != null && gridLayout.IsCellEmpty(cell))
                {
                    gridLayout.AddCellToRoom(selectedRoomId, cell);
                    e.Use();
                    Repaint();
                }
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                dragStartCell = null;
            }
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                string roomId = gridLayout.GetRoomAtCell(cell);
                if (!string.IsNullOrEmpty(roomId))
                {
                    gridLayout.RemoveRoom(roomId);
                    if (selectedRoomId == roomId)
                        selectedRoomId = null;
                    e.Use();
                    Repaint();
                }
            }
        }
        else if (gridMode == 1)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                string roomId = gridLayout.GetRoomAtCell(cell);
                if (!string.IsNullOrEmpty(roomId))
                {
                    List<Vector2Int> adjacentCells = gridLayout.GetAdjacentCells(cell);
                    foreach (var adjacentCell in adjacentCells)
                    {
                        string adjacentRoomId = gridLayout.GetRoomAtCell(adjacentCell);
                        if (!string.IsNullOrEmpty(adjacentRoomId) && adjacentRoomId != roomId)
                        {
                            if (gridLayout.TryAddDoor(cell, adjacentCell))
                            {
                                e.Use();
                                Repaint();
                                break;
                            }
                        }
                    }
                }
            }
        }
        else if (gridMode == 2)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                int targetFloor = currentFloor + 1;
                if (targetFloor < floors.Count)
                {
                    gridLayout.TryAddStairs(cell, currentFloor, targetFloor, currentStairsDirection);
                    e.Use();
                    Repaint();
                }
            }
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                gridLayout.RemoveStairsAtCell(cell);
                e.Use();
                Repaint();
            }
        }
        else if (gridMode == 3)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                string roomId = gridLayout.GetRoomAtCell(cell);
                if (!string.IsNullOrEmpty(roomId))
                {
                    selectedRoomId = roomId;
                    dragStartCell = cell;
                }
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && dragStartCell.HasValue && selectedRoomId != null)
            {
                if (gridLayout.IsCellEmpty(cell))
                {
                    gridLayout.AddCellToRoom(selectedRoomId, cell);
                    e.Use();
                    Repaint();
                }
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                dragStartCell = null;
            }
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                GridDoor doorAtCell = gridLayout.GetDoorAtCell(cell);
                if (doorAtCell != null)
                {
                    gridLayout.RemoveDoorBetween(doorAtCell.cell1, doorAtCell.cell2);
                    e.Use();
                    Repaint();
                }
                else
                {
                    string roomId = gridLayout.GetRoomAtCell(cell);
                    if (!string.IsNullOrEmpty(roomId))
                    {
                        gridLayout.RemoveCellFromRoom(roomId, cell);
                        dragStartCell = cell;
                        e.Use();
                        Repaint();
                    }
                }
            }
            else if (e.type == EventType.MouseDrag && e.button == 1 && dragStartCell.HasValue)
            {
                GridDoor doorAtCell = gridLayout.GetDoorAtCell(cell);
                if (doorAtCell != null)
                {
                    gridLayout.RemoveDoorBetween(doorAtCell.cell1, doorAtCell.cell2);
                }
                else
                {
                    string roomId = gridLayout.GetRoomAtCell(cell);
                    if (!string.IsNullOrEmpty(roomId))
                    {
                        gridLayout.RemoveCellFromRoom(roomId, cell);
                    }
                }
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseUp && e.button == 1)
            {
                dragStartCell = null;
            }
        }
    }
    
    private void DrawRoomsList()
    {
        EditorGUILayout.LabelField("📦 Pièces Créées", EditorStyles.boldLabel);
        
        gridScrollPosition = EditorGUILayout.BeginScrollView(gridScrollPosition, GUILayout.Height(200));
        
        foreach (var room in gridLayout.rooms)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            bool isSelected = selectedRoomId == room.id;
            Color originalColor = GUI.backgroundColor;
            if (isSelected)
            {
                GUI.backgroundColor = Color.green;
            }
            
            if (GUILayout.Button($"● {room.id}", GUILayout.Width(50)))
            {
                selectedRoomId = room.id;
            }
            
            GUI.backgroundColor = originalColor;
            
            room.displayName = EditorGUILayout.TextField(room.displayName);
            
            EditorGUILayout.EndHorizontal();
            
            if (isSelected)
            {
                EditorGUI.indentLevel++;
                
                Vector2Int gridSize = room.GetGridSize();
                EditorGUILayout.LabelField($"Taille grille : {gridSize.x} × {gridSize.y} cellules");
                EditorGUILayout.LabelField($"Cellules occupées : {room.cells.Count}");
                
                room.height = EditorGUILayout.Slider("Hauteur (m)", room.height, 0.5f, 10f);
                
                GUILayout.Space(5);
                EditorGUILayout.LabelField("📏 Hauteurs Sol/Plafond", EditorStyles.boldLabel);
                
                room.floorHeight = EditorGUILayout.Slider("Sol à (m)", room.floorHeight, 0f, 10f);
                room.ceilingHeight = EditorGUILayout.Slider("Plafond à (m)", room.ceilingHeight, room.floorHeight + 0.5f, room.floorHeight + 10f);
                
                EditorGUILayout.LabelField($"Espace utile : {room.ceilingHeight - room.floorHeight:F1}m", EditorStyles.miniLabel);
                
                room.previewColor = EditorGUILayout.ColorField("Couleur preview", room.previewColor);
                
                GUILayout.Space(5);
                EditorGUILayout.LabelField("📐 Dimensions personnalisées", EditorStyles.boldLabel);
                    
                    float maxWidth = room.GetMaxPossibleWidth(gridCellSize);
                    float maxDepth = room.GetMaxPossibleDepth(gridCellSize);
                    
                    EditorGUILayout.LabelField($"Taille maximale : {maxWidth:F1}m × {maxDepth:F1}m", EditorStyles.miniLabel);
                    
                    room.useCustomSize = EditorGUILayout.Toggle("Activer taille personnalisée", room.useCustomSize);
                    
                    if (room.useCustomSize)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.HelpBox($"Réduisez la taille de la salle tout en conservant ses {gridSize.x}×{gridSize.y} cellules sur la grille.", MessageType.Info);
                        room.customWidth = EditorGUILayout.Slider("Largeur (m)", room.customWidth, 0.3f, maxWidth);
                        room.customDepth = EditorGUILayout.Slider("Profondeur (m)", room.customDepth, 0.3f, maxDepth);
                        EditorGUI.indentLevel--;
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"Taille actuelle : {maxWidth:F1}m × {maxDepth:F1}m", EditorStyles.miniLabel);
                    }
                
                GUILayout.Space(5);
                EditorGUILayout.LabelField("🏗️ Géométrie", EditorStyles.boldLabel);
                
                room.hasFloor = EditorGUILayout.Toggle("Générer le sol", room.hasFloor);
                room.hasCeiling = EditorGUILayout.Toggle("Générer le plafond", room.hasCeiling);
                
                GUILayout.Space(5);
                EditorGUILayout.LabelField("🎨 Matériaux personnalisés", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Laissez vide pour utiliser les matériaux par défaut", MessageType.Info);
                
                room.wallMaterial = (Material)EditorGUILayout.ObjectField("Murs", room.wallMaterial, typeof(Material), false);
                
                bool showFloor = room.hasFloor;
                GUI.enabled = showFloor;
                room.floorMaterial = (Material)EditorGUILayout.ObjectField("Sol", room.floorMaterial, typeof(Material), false);
                GUI.enabled = true;
                
                bool showCeiling = room.hasCeiling;
                GUI.enabled = showCeiling;
                room.ceilingMaterial = (Material)EditorGUILayout.ObjectField("Plafond", room.ceilingMaterial, typeof(Material), false);
                GUI.enabled = true;
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawDoorsList()
    {
        EditorGUILayout.LabelField("🚪 Portes Créées", EditorStyles.boldLabel);
        
        if (gridLayout.doors.Count == 0)
        {
            EditorGUILayout.HelpBox("Aucune porte créée. Utilisez le mode '🚪 Placer Portes' pour en ajouter.", MessageType.Info);
            return;
        }
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        for (int i = 0; i < gridLayout.doors.Count; i++)
        {
            var door = gridLayout.doors[i];
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            string doorIcon = door.doorType == GridDoor.DoorType.Standard ? "🚪" : "🔲";
            string doorLabel = $"{doorIcon} {door.roomId1} ↔ {door.roomId2}";
            EditorGUILayout.LabelField(doorLabel, EditorStyles.boldLabel);
            if (GUILayout.Button("✖", GUILayout.Width(25)))
            {
                gridLayout.RemoveDoorBetween(door.cell1, door.cell2);
                Repaint();
                break;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.indentLevel++;
            
            door.doorType = (GridDoor.DoorType)EditorGUILayout.EnumPopup("Type", door.doorType);
            
            if (door.doorType == GridDoor.DoorType.Standard)
            {
                EditorGUILayout.LabelField("Dimensions", EditorStyles.miniLabel);
                EditorGUI.indentLevel++;
                door.width = EditorGUILayout.Slider("Largeur (m)", door.width, 0.3f, 2.5f);
                door.height = EditorGUILayout.Slider("Hauteur (m)", door.height, 0.3f, 3.5f);
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.LabelField("Dimensions du conduit", EditorStyles.miniLabel);
                EditorGUI.indentLevel++;
                door.width = EditorGUILayout.Slider("Largeur (m)", door.width, 0.3f, 1.5f);
                door.height = EditorGUILayout.Slider("Hauteur (m)", door.height, 0.3f, 1.5f);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.LabelField($"Position: {door.side}", EditorStyles.miniLabel);
            
            EditorGUI.indentLevel--;
            
            EditorGUILayout.EndVertical();
            
            if (i < gridLayout.doors.Count - 1)
            {
                GUILayout.Space(5);
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void PreviewGridLayout()
    {
        showGridPreview = !showGridPreview;
        SceneView.RepaintAll();
        
        if (showGridPreview)
        {
            Debug.Log("🔍 Preview activé - Visible dans la Scene View");
        }
        else
        {
            Debug.Log("Preview désactivé");
        }
    }
    
    private void GenerateFromGridLayout()
    {
        int totalRooms = 0;
        foreach (var floor in floors)
        {
            totalRooms += floor.rooms.Count;
        }
        
        if (totalRooms == 0)
        {
            EditorUtility.DisplayDialog("Erreur", "Aucune pièce à générer sur aucun étage !", "OK");
            return;
        }
        
        GameObject parent = new GameObject("GridLevel");
        lastGeneratedLevel = parent;
        
        Debug.Log($"[GenerateFromGridLayout] Début génération de {floors.Count} étage(s)");
        
        for (int floorIndex = 0; floorIndex < floors.Count; floorIndex++)
        {
            GridLayoutData floorData = floors[floorIndex];
            
            if (floorData.rooms.Count == 0)
            {
                Debug.Log($"  -> Étage {floorIndex + 1} : vide, ignoré");
                continue;
            }
            
            GameObject floorParent = new GameObject($"Floor_{floorIndex + 1}");
            floorParent.transform.SetParent(parent.transform);
            floorParent.transform.position = new Vector3(0, floorIndex * floorHeight, 0);
            
            Debug.Log($"  -> Étage {floorIndex + 1} : {floorData.rooms.Count} pièces à hauteur {floorIndex * floorHeight}m");
            
            foreach (var room in floorData.rooms)
            {
                GenerateRoomWithSegmentedWalls(room, floorParent, floorIndex);
            }
            
            foreach (var stairs in floorData.stairs)
            {
                GenerateStairs(stairs, floorParent, floorIndex);
            }
        }
        
        Selection.activeGameObject = parent;
        EditorGUIUtility.PingObject(parent);
        
        Debug.Log($"Niveau multi-étages généré : {floors.Count} étage(s), {totalRooms} pièces au total");
    }
    
    private void GenerateStairs(GridStairs stairs, GameObject parent, int floorIndex)
    {
        Vector3 worldPosition = stairs.GetWorldPosition(gridCellSize);
        worldPosition += new Vector3(gridCellSize / 2f, 0f, -gridCellSize / 2f);
        
        Quaternion rotation = stairs.GetWorldRotation();
        
        float stairsWidth = gridCellSize * 0.8f;
        float stairsDepth = gridCellSize * 0.8f;
        float totalHeight = floorHeight;
        int stepCount = Mathf.Max(8, Mathf.RoundToInt(floorHeight / 0.2f));
        
        Material material = stairsMaterial != null ? stairsMaterial : wallMaterial;
        
        GameObject stairsObj = LevelGenerator.CreateStairs(
            worldPosition,
            rotation,
            stairsWidth,
            stairsDepth,
            totalHeight,
            stepCount,
            material
        );
        
        stairsObj.transform.SetParent(parent.transform);
        stairsObj.transform.localPosition = worldPosition;
        
        Debug.Log($"Escalier généré à ({stairs.cell.x}, {stairs.cell.y}) avec {stepCount} marches");
    }
    
    private void GenerateRoomWithSegmentedWalls(GridRoom room, GameObject parent, int floorIndex)
    {
        GameObject roomObj = new GameObject(room.displayName);
        roomObj.transform.SetParent(parent.transform);
        roomObj.transform.localPosition = Vector3.zero;
        
        Material roomFloorMaterial = room.floorMaterial != null ? room.floorMaterial : floorMaterial;
        Material roomWallMaterial = room.wallMaterial != null ? room.wallMaterial : wallMaterial;
        Material roomCeilingMaterial = room.ceilingMaterial != null ? room.ceilingMaterial : ceilingMaterial;
        
        GridLayoutData floorData = floors[floorIndex];
        
        GenerateFloorAndCeilingForRoom(room, roomObj, roomFloorMaterial, roomCeilingMaterial);
        GenerateWallsForRoomCellBased(room, roomObj, room.height, roomWallMaterial, floorData);
    }
    
    private void GenerateFloorAndCeilingForRoom(GridRoom room, GameObject roomObj, Material floorMat, Material ceilingMat)
    {
        bool shouldGenerateFloor = globalGenerateFloor && room.hasFloor;
        bool shouldGenerateCeiling = globalGenerateCeiling && room.hasCeiling;
        
        float floorY = 0f;
        float ceilingY = room.height;
        
        if (room.useCustomSize)
        {
            Vector2Int minCell = room.GetMinCell();
            Vector2Int maxCell = room.GetMaxCell();
            float centerX = (minCell.x + maxCell.x) * gridCellSize / 2f;
            float centerZ = -(minCell.y + maxCell.y) * gridCellSize / 2f;
            
            float roomWidth = room.GetActualWidth(gridCellSize);
            float roomDepth = room.GetActualDepth(gridCellSize);
            
            if (shouldGenerateFloor)
            {
                ProBuilderMesh floor = LevelGenerator.CreateFloorForRoom(
                    new Vector3(roomWidth, room.height, roomDepth),
                    floorMat
                );
                floor.transform.SetParent(roomObj.transform);
                floor.transform.localPosition = new Vector3(centerX, floorY, centerZ);
                floor.name = $"Floor_{room.id}";
            }
            
            if (shouldGenerateCeiling)
            {
                ProBuilderMesh ceiling = LevelGenerator.CreateCeilingForRoom(
                    new Vector3(roomWidth, room.height, roomDepth),
                    ceilingY,
                    ceilingMat
                );
                ceiling.transform.SetParent(roomObj.transform);
                ceiling.transform.localPosition = new Vector3(centerX, floorY, centerZ);
                ceiling.name = $"Ceiling_{room.id}";
            }
        }
        else
        {
            foreach (var cell in room.cells)
            {
                float x = cell.x * gridCellSize;
                float z = -cell.y * gridCellSize;
                
                if (shouldGenerateFloor)
                {
                    ProBuilderMesh floor = LevelGenerator.CreateFloorForRoom(
                        new Vector3(gridCellSize, room.height, gridCellSize),
                        floorMat
                    );
                    floor.transform.SetParent(roomObj.transform);
                    floor.transform.localPosition = new Vector3(x, floorY, z);
                    floor.name = $"Floor_Cell_{cell.x}_{cell.y}";
                }
                
                if (shouldGenerateCeiling)
                {
                    ProBuilderMesh ceiling = LevelGenerator.CreateCeilingForRoom(
                        new Vector3(gridCellSize, room.height, gridCellSize),
                        ceilingY,
                        ceilingMat
                    );
                    ceiling.transform.SetParent(roomObj.transform);
                    ceiling.transform.localPosition = new Vector3(x, floorY, z);
                    ceiling.name = $"Ceiling_Cell_{cell.x}_{cell.y}";
                }
            }
        }
    }
    
    private void GenerateWallsForRoomCellBased(GridRoom room, GameObject roomObj, float roomHeight, Material roomWallMaterial, GridLayoutData floorData)
    {
        if (room.useCustomSize)
        {
            GenerateWallsForCustomSizeRoom(room, roomObj, roomHeight, roomWallMaterial);
        }
        else
        {
            foreach (var cell in room.cells)
            {
                Vector2Int[] neighbors = {
                    new Vector2Int(cell.x, cell.y - 1),
                    new Vector2Int(cell.x, cell.y + 1),
                    new Vector2Int(cell.x + 1, cell.y),
                    new Vector2Int(cell.x - 1, cell.y)
                };
                
                string[] sides = { "North", "South", "East", "West" };
                
                for (int i = 0; i < 4; i++)
                {
                    Vector2Int neighbor = neighbors[i];
                    string side = sides[i];
                    
                    bool shouldCreateWall = !room.cells.Contains(neighbor);
                    
                    if (shouldCreateWall)
                    {
                        GridDoor door = GetDoorOnCellSide(room.id, cell, side, floorData);
                        CreateWallForCell(room, roomObj, cell, side, roomHeight, roomWallMaterial, door);
                    }
                }
            }
        }
    }
    
    private void GenerateWallsForCustomSizeRoom(GridRoom room, GameObject roomObj, float roomHeight, Material roomWallMaterial)
    {
        Vector2Int minCell = room.GetMinCell();
        Vector2Int maxCell = room.GetMaxCell();
        float centerX = (minCell.x + maxCell.x) * gridCellSize / 2f;
        float centerZ = -(minCell.y + maxCell.y) * gridCellSize / 2f;
        
        float roomWidth = room.GetActualWidth(gridCellSize);
        float roomDepth = room.GetActualDepth(gridCellSize);
        
        float halfWidth = roomWidth / 2f;
        float halfDepth = roomDepth / 2f;
        
        float baseY = 0f;
        
        Vector3 northStart = new Vector3(centerX - halfWidth, baseY, centerZ + halfDepth);
        Vector3 northEnd = new Vector3(centerX + halfWidth, baseY, centerZ + halfDepth);
        
        Vector3 southStart = new Vector3(centerX + halfWidth, baseY, centerZ - halfDepth);
        Vector3 southEnd = new Vector3(centerX - halfWidth, baseY, centerZ - halfDepth);
        
        Vector3 eastStart = new Vector3(centerX + halfWidth, baseY, centerZ + halfDepth);
        Vector3 eastEnd = new Vector3(centerX + halfWidth, baseY, centerZ - halfDepth);
        
        Vector3 westStart = new Vector3(centerX - halfWidth, baseY, centerZ - halfDepth);
        Vector3 westEnd = new Vector3(centerX - halfWidth, baseY, centerZ + halfDepth);
        
        ProBuilderMesh northWall = LevelGenerator.CreateWallSegment(northStart, northEnd, roomHeight, roomWallMaterial, $"Wall_North_{room.id}");
        northWall.transform.SetParent(roomObj.transform);
        northWall.transform.localPosition = Vector3.zero;
        
        ProBuilderMesh southWall = LevelGenerator.CreateWallSegment(southStart, southEnd, roomHeight, roomWallMaterial, $"Wall_South_{room.id}");
        southWall.transform.SetParent(roomObj.transform);
        southWall.transform.localPosition = Vector3.zero;
        
        ProBuilderMesh eastWall = LevelGenerator.CreateWallSegment(eastStart, eastEnd, roomHeight, roomWallMaterial, $"Wall_East_{room.id}");
        eastWall.transform.SetParent(roomObj.transform);
        eastWall.transform.localPosition = Vector3.zero;
        
        ProBuilderMesh westWall = LevelGenerator.CreateWallSegment(westStart, westEnd, roomHeight, roomWallMaterial, $"Wall_West_{room.id}");
        westWall.transform.SetParent(roomObj.transform);
        westWall.transform.localPosition = Vector3.zero;
    }
    
    private GridDoor GetDoorOnCellSide(string roomId, Vector2Int cell, string side, GridLayoutData floorData)
    {
        foreach (var door in floorData.doors)
        {
            if (door.roomId1 != roomId && door.roomId2 != roomId)
                continue;
            
            bool isRoom1 = door.roomId1 == roomId;
            Vector2Int doorCell = isRoom1 ? door.cell1 : door.cell2;
            
            if (doorCell != cell)
                continue;
            
            string doorSideForThisRoom = "";
            
            switch (door.side)
            {
                case GridDoor.Side.North:
                    doorSideForThisRoom = isRoom1 ? "North" : "South";
                    break;
                case GridDoor.Side.South:
                    doorSideForThisRoom = isRoom1 ? "South" : "North";
                    break;
                case GridDoor.Side.East:
                    doorSideForThisRoom = isRoom1 ? "East" : "West";
                    break;
                case GridDoor.Side.West:
                    doorSideForThisRoom = isRoom1 ? "West" : "East";
                    break;
            }
            
            if (doorSideForThisRoom == side)
                return door;
        }
        
        return null;
    }
    
    private void CreateWallForCell(GridRoom room, GameObject roomObj, Vector2Int cell, string side, float roomHeight, Material roomWallMaterial, GridDoor door)
    {
        float x = cell.x * gridCellSize;
        float z = -cell.y * gridCellSize;
        
        float baseY = 0f;
        
        Vector3 start = Vector3.zero;
        Vector3 end = Vector3.zero;
        
        float halfCell = gridCellSize / 2f;
        
        switch (side)
        {
            case "North":
                start = new Vector3(x - halfCell, baseY, z + halfCell);
                end = new Vector3(x + halfCell, baseY, z + halfCell);
                break;
            case "South":
                start = new Vector3(x + halfCell, baseY, z - halfCell);
                end = new Vector3(x - halfCell, baseY, z - halfCell);
                break;
            case "East":
                start = new Vector3(x + halfCell, baseY, z + halfCell);
                end = new Vector3(x + halfCell, baseY, z - halfCell);
                break;
            case "West":
                start = new Vector3(x - halfCell, baseY, z - halfCell);
                end = new Vector3(x - halfCell, baseY, z + halfCell);
                break;
        }
        
        if (door != null)
        {
            CreateWallWithDoor(roomObj, start, end, roomHeight, roomWallMaterial, door, $"Wall_{side}_Cell_{cell.x}_{cell.y}");
        }
        else
        {
            ProBuilderMesh wall = LevelGenerator.CreateWallSegment(start, end, roomHeight, roomWallMaterial, $"Wall_{side}_Cell_{cell.x}_{cell.y}");
            wall.transform.SetParent(roomObj.transform);
            wall.transform.localPosition = Vector3.zero;
        }
    }
    
    private void CreateWallWithDoor(GameObject roomObj, Vector3 start, Vector3 end, float roomHeight, Material roomWallMaterial, GridDoor door, string wallName)
    {
        Vector3 direction = (end - start).normalized;
        float wallLength = Vector3.Distance(start, end);
        
        float doorWidth = door.width;
        float doorHeight = door.height;
        float wallThickness = 0.2f;
        
        float leftRightWidth = (wallLength - doorWidth) / 2f;
        float topHeight = roomHeight - doorHeight;
        
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        
        float doorBottomOffset = 0f;
        if (door.doorType == GridDoor.DoorType.Conduit)
        {
            doorBottomOffset = roomHeight * 0.6f;
            topHeight = roomHeight - doorHeight - doorBottomOffset;
        }
        
        if (leftRightWidth > 0.05f)
        {
            Vector3 leftStart = start;
            Vector3 leftEnd = start + direction * leftRightWidth;
            ProBuilderMesh leftWall = LevelGenerator.CreateWallSegment(leftStart, leftEnd, roomHeight, roomWallMaterial, $"{wallName}_Left");
            leftWall.transform.SetParent(roomObj.transform);
            
            Vector3 rightStart = end - direction * leftRightWidth;
            Vector3 rightEnd = end;
            ProBuilderMesh rightWall = LevelGenerator.CreateWallSegment(rightStart, rightEnd, roomHeight, roomWallMaterial, $"{wallName}_Right");
            rightWall.transform.SetParent(roomObj.transform);
        }
        
        if (doorBottomOffset > 0.05f)
        {
            Vector3 bottomStart = start + direction * Mathf.Max(leftRightWidth, 0f);
            Vector3 bottomEnd = end - direction * Mathf.Max(leftRightWidth, 0f);
            float actualDoorWidth = Vector3.Distance(bottomStart, bottomEnd);
            Vector3 bottomCenter = (bottomStart + bottomEnd) / 2f;
            
            GameObject bottomWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottomWall.name = $"{wallName}_Bottom";
            bottomWall.transform.SetParent(roomObj.transform);
            bottomWall.transform.position = bottomCenter + Vector3.up * (doorBottomOffset / 2f);
            bottomWall.transform.localScale = new Vector3(actualDoorWidth, doorBottomOffset, wallThickness);
            bottomWall.transform.rotation = Quaternion.LookRotation(perpendicular);
            
            MeshRenderer bottomRenderer = bottomWall.GetComponent<MeshRenderer>();
            if (bottomRenderer != null && roomWallMaterial != null)
            {
                bottomRenderer.sharedMaterial = roomWallMaterial;
            }
        }
        
        if (topHeight > 0.05f)
        {
            Vector3 doorTopStart = start + direction * Mathf.Max(leftRightWidth, 0f);
            Vector3 doorTopEnd = end - direction * Mathf.Max(leftRightWidth, 0f);
            float actualDoorWidth = Vector3.Distance(doorTopStart, doorTopEnd);
            Vector3 doorTopCenter = (doorTopStart + doorTopEnd) / 2f;
            
            GameObject topWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topWall.name = $"{wallName}_Top";
            topWall.transform.SetParent(roomObj.transform);
            topWall.transform.position = doorTopCenter + Vector3.up * (doorBottomOffset + doorHeight + topHeight / 2f);
            topWall.transform.localScale = new Vector3(actualDoorWidth, topHeight, wallThickness);
            topWall.transform.rotation = Quaternion.LookRotation(perpendicular);
            
            MeshRenderer topRenderer = topWall.GetComponent<MeshRenderer>();
            if (topRenderer != null && roomWallMaterial != null)
            {
                topRenderer.sharedMaterial = roomWallMaterial;
            }
        }
    }
    
    private void DrawGridPreviewInScene()
    {
        float cellSize = gridCellSize;
        int totalRooms = 0;
        int totalDoors = 0;
        
        for (int floorIndex = 0; floorIndex < floors.Count; floorIndex++)
        {
            GridLayoutData floorData = floors[floorIndex];
            float floorYOffset = floorIndex * floorHeight;
            
            float alpha = (floorIndex == currentFloor) ? 0.5f : 0.15f;
            
            totalRooms += floorData.rooms.Count;
            totalDoors += floorData.doors.Count;
            
            foreach (var room in floorData.rooms)
            {
                Color roomColor = room.previewColor;
                roomColor.a = alpha;
                
                if (room.useCustomSize)
                {
                    Vector2Int minCell = room.GetMinCell();
                    Vector2Int maxCell = room.GetMaxCell();
                    float centerX = (minCell.x + maxCell.x) * cellSize / 2f;
                    float centerZ = -(minCell.y + maxCell.y) * cellSize / 2f;
                    
                    float roomWidth = room.GetActualWidth(cellSize);
                    float roomDepth = room.GetActualDepth(cellSize);
                    
                    float baseY = room.floorHeight + floorYOffset;
                    Vector3 roomCenter = new Vector3(centerX, baseY + room.height / 2f, centerZ);
                    Vector3 roomSize = new Vector3(roomWidth, room.height, roomDepth);
                    
                    Color displayColor = roomColor;
                    
                    Handles.color = displayColor;
                    Handles.DrawWireCube(roomCenter, roomSize);
                    
                    Handles.color = new Color(displayColor.r, displayColor.g, displayColor.b, alpha * 0.5f);
                    
                    Vector3 halfSize = roomSize / 2f;
                    Vector3[] floorCorners = new Vector3[4]
                    {
                        roomCenter + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
                        roomCenter + new Vector3(halfSize.x, -halfSize.y, -halfSize.z),
                        roomCenter + new Vector3(halfSize.x, -halfSize.y, halfSize.z),
                        roomCenter + new Vector3(-halfSize.x, -halfSize.y, halfSize.z)
                    };
                    Handles.DrawSolidRectangleWithOutline(floorCorners, new Color(displayColor.r, displayColor.g, displayColor.b, alpha * 0.3f), new Color(displayColor.r, displayColor.g, displayColor.b, 1f));
                    
                    if (floorIndex == currentFloor)
                    {
                        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                        labelStyle.normal.textColor = Color.white;
                        labelStyle.fontStyle = FontStyle.Bold;
                        labelStyle.fontSize = 14;
                        labelStyle.alignment = TextAnchor.MiddleCenter;
                        
                        Handles.Label(roomCenter + Vector3.up * 0.5f, $"{room.displayName}\n{roomWidth:F1}m × {roomDepth:F1}m × {room.height:F1}m", labelStyle);
                    }
                }
                else
                {
                    foreach (var cell in room.cells)
                    {
                        Vector3 cellCenter = new Vector3(cell.x * cellSize + cellSize / 2f, floorYOffset, -cell.y * cellSize - cellSize / 2f);
                        
                        Handles.color = roomColor;
                        Vector3[] floorCorners = new Vector3[4]
                        {
                            cellCenter + new Vector3(-cellSize/2, 0, -cellSize/2),
                            cellCenter + new Vector3(cellSize/2, 0, -cellSize/2),
                            cellCenter + new Vector3(cellSize/2, 0, cellSize/2),
                            cellCenter + new Vector3(-cellSize/2, 0, cellSize/2)
                        };
                        Handles.DrawSolidRectangleWithOutline(floorCorners, roomColor, Color.gray);
                        
                        Handles.color = new Color(roomColor.r, roomColor.g, roomColor.b, alpha);
                        Vector3 wallHeight = Vector3.up * room.height;
                        for (int i = 0; i < 4; i++)
                        {
                            Handles.DrawLine(floorCorners[i], floorCorners[i] + wallHeight);
                            Handles.DrawLine(floorCorners[i] + wallHeight, floorCorners[(i + 1) % 4] + wallHeight);
                        }
                    }
                    
                    if (floorIndex == currentFloor)
                    {
                        Vector2Int min = room.GetBoundsMin();
                        Vector2Int max = room.GetBoundsMax();
                        Vector3 roomCenter = new Vector3((min.x + max.x + 1) * cellSize / 2f, floorYOffset + room.height / 2f, -(min.y + max.y + 1) * cellSize / 2f);
                        
                        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                        labelStyle.normal.textColor = Color.white;
                        labelStyle.fontStyle = FontStyle.Bold;
                        labelStyle.fontSize = 14;
                        labelStyle.alignment = TextAnchor.MiddleCenter;
                        
                        Handles.Label(roomCenter + Vector3.up * 0.5f, room.displayName, labelStyle);
                    }
                }
            }
            
            foreach (var door in floorData.doors)
            {
                Vector3 doorCenter = door.GetWorldPosition(cellSize);
                doorCenter.y += floorYOffset + 1f;
                
                string doorIcon = door.doorType == GridDoor.DoorType.Standard ? "🚪" : "🔲";
                Color doorColor = door.doorType == GridDoor.DoorType.Standard ? Color.yellow : Color.cyan;
                doorColor.a = alpha;
                
                Handles.color = doorColor;
                Handles.DrawWireCube(doorCenter, new Vector3(door.width, door.height, 0.2f));
                
                if (floorIndex == currentFloor)
                {
                    GUIStyle doorStyle = new GUIStyle(GUI.skin.label);
                    doorStyle.normal.textColor = doorColor;
                    doorStyle.fontStyle = FontStyle.Bold;
                    doorStyle.fontSize = 12;
                    doorStyle.alignment = TextAnchor.MiddleCenter;
                    
                    Handles.Label(doorCenter + Vector3.up * 1.5f, doorIcon, doorStyle);
                }
            }
            
            foreach (var stairs in floorData.stairs)
            {
                Vector3 stairsCenter = stairs.GetWorldPosition(cellSize);
                stairsCenter += new Vector3(cellSize / 2f, floorYOffset, -cellSize / 2f);
                
                Color stairsColor = new Color(1f, 0.5f, 0f, alpha);
                
                Handles.color = stairsColor;
                Vector3 stairsSize = new Vector3(cellSize * 0.8f, floorHeight, cellSize * 0.8f);
                Handles.DrawWireCube(stairsCenter + Vector3.up * floorHeight / 2f, stairsSize);
                
                if (floorIndex == currentFloor)
                {
                    GUIStyle stairsStyle = new GUIStyle(GUI.skin.label);
                    stairsStyle.normal.textColor = new Color(1f, 0.5f, 0f);
                    stairsStyle.fontStyle = FontStyle.Bold;
                    stairsStyle.fontSize = 16;
                    stairsStyle.alignment = TextAnchor.MiddleCenter;
                    
                    Handles.Label(stairsCenter + Vector3.up * floorHeight / 2f, "🪜", stairsStyle);
                }
            }
        }
        
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 280, 150));
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("🔍 MODE PREVIEW - MULTI-ÉTAGES", EditorStyles.boldLabel);
        GUILayout.Label($"Étages: {floors.Count}");
        GUILayout.Label($"Étage actif: {currentFloor + 1}");
        GUILayout.Label($"Pièces totales: {totalRooms}");
        GUILayout.Label($"Portes totales: {totalDoors}");
        int totalStairs = floors.Sum(f => f.stairs.Count);
        GUILayout.Label($"Escaliers totaux: {totalStairs}");
        GUILayout.Label($"Taille cellule: {cellSize}m");
        GUILayout.EndVertical();
        GUILayout.EndArea();
        Handles.EndGUI();
    }
    
    private void SaveCurrentLayout()
    {
        if (currentPreset == null)
        {
            CreateNewPreset();
            return;
        }
        
        currentPreset.SaveCurrentState(
            gridWidth, gridHeight, gridCellSize, floorHeight,
            wallMaterial, floorMaterial, ceilingMaterial, stairsMaterial,
            globalGenerateFloor, globalGenerateCeiling,
            floors
        );
        
        EditorUtility.SetDirty(currentPreset);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"✓ Configuration sauvegardée dans {currentPreset.name}");
        EditorUtility.DisplayDialog("Sauvegarde réussie", 
            $"La configuration a été sauvegardée dans :\n{AssetDatabase.GetAssetPath(currentPreset)}", 
            "OK");
    }
    
    private void LoadLayoutFromPreset()
    {
        if (currentPreset == null)
        {
            EditorUtility.DisplayDialog("Erreur", "Aucun preset sélectionné !", "OK");
            return;
        }
        
        bool confirm = EditorUtility.DisplayDialog("Charger la configuration ?", 
            $"Voulez-vous charger la configuration depuis :\n{currentPreset.name}\n\n" +
            "⚠️ Cela remplacera votre configuration actuelle !",
            "Charger", "Annuler");
        
        if (!confirm) return;
        
        gridWidth = currentPreset.gridWidth;
        gridHeight = currentPreset.gridHeight;
        gridCellSize = currentPreset.gridCellSize;
        floorHeight = currentPreset.floorHeight;
        
        wallMaterial = currentPreset.wallMaterial;
        floorMaterial = currentPreset.floorMaterial;
        ceilingMaterial = currentPreset.ceilingMaterial;
        stairsMaterial = currentPreset.stairsMaterial;
        
        globalGenerateFloor = currentPreset.globalGenerateFloor;
        globalGenerateCeiling = currentPreset.globalGenerateCeiling;
        
        floors = new List<GridLayoutData>();
        foreach (var floorData in currentPreset.floors)
        {
            GridLayoutData newFloor = new GridLayoutData(floorData.gridWidth, floorData.gridHeight);
            
            foreach (var room in floorData.rooms)
            {
                GridRoom newRoom = new GridRoom
                {
                    id = room.id,
                    displayName = room.displayName,
                    cells = new List<Vector2Int>(room.cells),
                    useCustomSize = room.useCustomSize,
                    customWidth = room.customWidth,
                    customDepth = room.customDepth,
                    height = room.height,
                    floorHeight = room.floorHeight,
                    ceilingHeight = room.ceilingHeight,
                    hasFloor = room.hasFloor,
                    hasCeiling = room.hasCeiling,
                    previewColor = room.previewColor,
                    wallMaterial = room.wallMaterial,
                    floorMaterial = room.floorMaterial,
                    ceilingMaterial = room.ceilingMaterial
                };
                newFloor.rooms.Add(newRoom);
            }
            
            foreach (var door in floorData.doors)
            {
                GridDoor newDoor = new GridDoor(
                    door.roomId1,
                    door.roomId2,
                    door.cell1,
                    door.cell2,
                    door.side
                )
                {
                    doorType = door.doorType,
                    width = door.width,
                    height = door.height
                };
                newFloor.doors.Add(newDoor);
            }
            
            foreach (var stairsItem in floorData.stairs)
            {
                GridStairs newStairs = new GridStairs(
                    stairsItem.cell,
                    stairsItem.fromFloor,
                    stairsItem.toFloor,
                    stairsItem.direction
                );
                newFloor.stairs.Add(newStairs);
            }
            
            newFloor.RebuildOccupancy();
            floors.Add(newFloor);
        }
        
        if (floors.Count == 0)
        {
            floors.Add(new GridLayoutData(gridWidth, gridHeight));
        }
        
        currentFloor = 0;
        selectedRoomId = null;
        
        Debug.Log($"✓ Configuration chargée depuis {currentPreset.name}");
        EditorUtility.DisplayDialog("Chargement réussi", 
            $"La configuration a été chargée depuis :\n{currentPreset.name}\n\n" +
            $"• {floors.Count} étage(s)\n" +
            $"• {floors.Sum(f => f.rooms.Count)} pièce(s)\n" +
            $"• {floors.Sum(f => f.doors.Count)} porte(s)\n" +
            $"• {floors.Sum(f => f.stairs.Count)} escalier(s)",
            "OK");
        
        Repaint();
    }
    
    private void CreateNewPreset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Créer un nouveau Preset",
            "New Level Preset",
            "asset",
            "Choisissez où sauvegarder le nouveau preset de niveau"
        );
        
        if (string.IsNullOrEmpty(path)) return;
        
        LevelLayoutPreset newPreset = ScriptableObject.CreateInstance<LevelLayoutPreset>();
        newPreset.metadata.presetName = System.IO.Path.GetFileNameWithoutExtension(path);
        newPreset.metadata.author = System.Environment.UserName;
        newPreset.metadata.creationDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        newPreset.SaveCurrentState(
            gridWidth, gridHeight, gridCellSize, floorHeight,
            wallMaterial, floorMaterial, ceilingMaterial, stairsMaterial,
            globalGenerateFloor, globalGenerateCeiling,
            floors
        );
        
        AssetDatabase.CreateAsset(newPreset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        currentPreset = newPreset;
        
        Debug.Log($"✓ Nouveau preset créé : {path}");
        EditorUtility.DisplayDialog("Preset créé", 
            $"Le nouveau preset a été créé avec succès !\n\n{path}", 
            "OK");
        
        EditorGUIUtility.PingObject(newPreset);
    }
    }

#endif
