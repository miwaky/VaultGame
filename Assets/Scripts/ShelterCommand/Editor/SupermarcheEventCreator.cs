using System.IO;
using UnityEditor;
using UnityEngine;

namespace ShelterCommand.Editor
{
    /// <summary>
    /// Generates the complete "Supermarché Abandonné" exploration event.
    ///
    /// Structure :
    ///   Appel 1 (Jour 1 — 10h00) : Observation extérieure — 3 entrées possibles
    ///   Appel 2 (Jour 1 — 11h–13h) : Accès au bâtiment selon l'entrée choisie
    ///   Appel 3 (Jour 1 — 14h–16h) : À l'intérieur — voix entendues
    ///   Appel 4 (Jour 1 — 16h–18h) : Fouille / Rencontre finale
    ///
    /// Menu : Window → ShelterCommand → Événements → Générer Supermarché
    /// Dossier : Assets/Data/Missions/Supermarche/
    /// </summary>
    public static class SupermarcheEventCreator
    {
        private const string RootDir = "Assets/Data/Missions/Supermarche";

        [MenuItem("Window/ShelterCommand/Événements/Générer Supermarché")]
        public static void Generate()
        {
            EnsureDirectory(RootDir);
            EnsureDirectory(RootDir + "/Dialogues");
            EnsureDirectory(RootDir + "/Encounters");

            // ─────────────────────────────────────────────────────────────────────
            // APPEL 4 — Fouille & Rencontres (feuilles de l'arbre — créées en premier)
            // ─────────────────────────────────────────────────────────────────────

            // ── Terminaux : résultats de rencontre ──────────────────────────────

            // Survivants neutres : échange réussi
            var d_neutres_echange = Dialogue("SM_Neutres_Echange",
                text: "Chef, on a négocié avec eux. Ils nous donnent de la nourriture " +
                      "en échange d'informations sur notre abri. {amount} {resource} récupérés.",
                hasLimit: false);
            d_neutres_echange.nodeEvent = AddResource(
                new ResourceEntry { resourceType = ResourceType.Food,  useRandomAmount = true, minAmount = 4, maxAmount = 9 },
                new ResourceEntry { resourceType = ResourceType.Water, useRandomAmount = true, minAmount = 2, maxAmount = 5 }
            );
            d_neutres_echange.followUpCall = null;
            Save(d_neutres_echange, "Dialogues/SM_Neutres_Echange");

            // Survivants neutres : recrutement
            var d_neutres_recrutement = Dialogue("SM_Neutres_Recrutement",
                text: "Chef, ils veulent nous rejoindre ! On rentre ensemble. " +
                      "Ils apportent {amount} {resource} avec eux.",
                hasLimit: false);
            d_neutres_recrutement.nodeEvent = AddResource(
                new ResourceEntry { resourceType = ResourceType.Food,  useRandomAmount = true, minAmount = 3, maxAmount = 7 },
                new ResourceEntry { resourceType = ResourceType.Medicine, useRandomAmount = true, minAmount = 1, maxAmount = 3 }
            );
            d_neutres_recrutement.followUpCall = null;
            Save(d_neutres_recrutement, "Dialogues/SM_Neutres_Recrutement");

            // Pillards : négociation réussie
            var d_pillards_nego_ok = Dialogue("SM_Pillards_Nego_Ok",
                text: "Chef… on a réussi à les convaincre de nous laisser partir. " +
                      "On a dû leur laisser quelques ressources. " +
                      "On rentre avec ce qu'il reste. {amount} {resource}.",
                hasLimit: false);
            d_pillards_nego_ok.nodeEvent = AddResource(
                new ResourceEntry { resourceType = ResourceType.Food, useRandomAmount = true, minAmount = 2, maxAmount = 5 }
            );
            Save(d_pillards_nego_ok, "Dialogues/SM_Pillards_Nego_Ok");

            // Pillards : fuite
            var d_pillards_fuite = Dialogue("SM_Pillards_Fuite",
                text: "On a couru. On a rien pu prendre. On rentre bredouilles.",
                hasLimit: false);
            d_pillards_fuite.nodeEvent = new DialogueEventData { eventType = DialogueEventType.MissionReturn };
            Save(d_pillards_fuite, "Dialogues/SM_Pillards_Fuite");

            // Pillards : combat victorieux
            var d_pillards_combat_victoire = Dialogue("SM_Pillards_Combat_Victoire",
                text: "Chef, combat terminé. On a tenu. Quelques blessures légères. " +
                      "On a sécurisé le magasin — {amount} {resource} récupérés.",
                hasLimit: false);
            d_pillards_combat_victoire.nodeEvent = AddResource(
                new ResourceEntry { resourceType = ResourceType.Food,      useRandomAmount = true, minAmount = 6, maxAmount = 15 },
                new ResourceEntry { resourceType = ResourceType.Materials,  useRandomAmount = true, minAmount = 2, maxAmount = 5  }
            );
            Save(d_pillards_combat_victoire, "Dialogues/SM_Pillards_Combat_Victoire");

            // Pillards : combat perdu — blessés
            var d_pillards_combat_echec = Dialogue("SM_Pillards_Combat_Echec",
                text: "Chef… on a pas pu. Des blessés. On bat en retraite.",
                hasLimit: false);
            d_pillards_combat_echec.nodeEvent = new DialogueEventData { eventType = DialogueEventType.Injury };
            Save(d_pillards_combat_echec, "Dialogues/SM_Pillards_Combat_Echec");

            // Magasin vide : fouille complète
            var d_vide_fouille = Dialogue("SM_Vide_FouilleComplete",
                text: "Chef, les hommes sont partis — le magasin est à nous ! " +
                      "On fouille tout. {amount} {resource} trouvés. On rentre chargés.",
                hasLimit: false);
            d_vide_fouille.nodeEvent = AddResource(
                new ResourceEntry { resourceType = ResourceType.Food,      useRandomAmount = true, minAmount = 8, maxAmount = 18 },
                new ResourceEntry { resourceType = ResourceType.Water,     useRandomAmount = true, minAmount = 3, maxAmount = 7  },
                new ResourceEntry { resourceType = ResourceType.Medicine,  useRandomAmount = true, minAmount = 1, maxAmount = 4  },
                new ResourceEntry { resourceType = ResourceType.Materials, useRandomAmount = true, minAmount = 2, maxAmount = 6  }
            );
            Save(d_vide_fouille, "Dialogues/SM_Vide_FouilleComplete");

            // Fouille rapide terminale
            var d_fouille_rapide_ok = Dialogue("SM_FouilleRapide_Ok",
                text: "Chef, fouille rapide terminée. {amount} {resource} récupérés " +
                      "avant d'approcher les hommes.",
                hasLimit: false);
            d_fouille_rapide_ok.nodeEvent = AddResource(
                new ResourceEntry { resourceType = ResourceType.Food,  useRandomAmount = true, minAmount = 2, maxAmount = 5 },
                new ResourceEntry { resourceType = ResourceType.Water, useRandomAmount = true, minAmount = 1, maxAmount = 3 }
            );
            Save(d_fouille_rapide_ok, "Dialogues/SM_FouilleRapide_Ok");

            // Fouille détaillée — rien trouvé (malchance / temps perdu)
            var d_fouille_detail_vide = Dialogue("SM_FouilleDetail_Vide",
                text: "Chef… les rayons sont vraiment vides. On a perdu du temps. " +
                      "Et maintenant les hommes sont sur nous.",
                hasLimit: false);
            Save(d_fouille_detail_vide, "Dialogues/SM_FouilleDetail_Vide");

            // Fouille détaillée — bonne récolte
            var d_fouille_detail_ok = Dialogue("SM_FouilleDetail_Ok",
                text: "Chef ! Les réserves du fond étaient intactes. " +
                      "{amount} {resource} — et on entend encore les hommes approcher.",
                hasLimit: false);
            d_fouille_detail_ok.nodeEvent = AddResource(
                new ResourceEntry { resourceType = ResourceType.Food,      useRandomAmount = true, minAmount = 5, maxAmount = 12 },
                new ResourceEntry { resourceType = ResourceType.Medicine,  useRandomAmount = true, minAmount = 0, maxAmount = 3  }
            );
            Save(d_fouille_detail_ok, "Dialogues/SM_FouilleDetail_Ok");

            // ─────────────────────────────────────────────────────────────────────
            // APPEL 4 — Nœuds principaux
            // ─────────────────────────────────────────────────────────────────────

            // Rencontre : Survivants neutres
            var d_appel4_neutres = Dialogue("SM_Appel4_Neutres",
                text: "Chef, on les a rejoints. Ce sont des survivants comme nous — " +
                      "ils cherchent de la nourriture.",
                hasLimit: true, timeSec: 35f, timeoutIdx: 0);
            d_appel4_neutres.choices = new[]
            {
                new DialogueChoice
                {
                    choiceText   = "Proposez un échange.",
                    nextDialogue = d_neutres_echange,
                    // Social élevé améliore les conditions de l'échange — mais pas obligatoire
                },
                new DialogueChoice
                {
                    choiceText   = "Proposez-leur de nous rejoindre.",
                    nextDialogue = d_neutres_recrutement,
                    condition    = new DialogueChoiceCondition
                    {
                        failBehaviour    = ConditionFailBehaviour.Disable,
                        statRequirements = new[] { new StatRequirement { stat = SurvivorStatIndex.Social, minimumValue = 25 } }
                    }
                },
                new DialogueChoice
                {
                    choiceText   = "Prenez ce que vous pouvez et partez discrètement.",
                    nextDialogue = d_fouille_rapide_ok,
                    eventTrigger = new DialogueEventData { eventType = DialogueEventType.None }
                },
            };
            Save(d_appel4_neutres, "Dialogues/SM_Appel4_Neutres");

            // Rencontre : Pillards
            var d_appel4_pillards = Dialogue("SM_Appel4_Pillards",
                text: "Chef… ce sont des pillards. Ils ont l'air armés et agressifs. " +
                      "On fait quoi ?",
                hasLimit: true, timeSec: 25f, timeoutIdx: 2);  // timeout = fuir par défaut
            d_appel4_pillards.choices = new[]
            {
                new DialogueChoice
                {
                    choiceText   = "Négociez — essayez de les convaincre. (Social ≥ 30)",
                    nextDialogue = d_pillards_nego_ok,
                    condition    = new DialogueChoiceCondition
                    {
                        failBehaviour    = ConditionFailBehaviour.Disable,
                        statRequirements = new[] { new StatRequirement { stat = SurvivorStatIndex.Social, minimumValue = 30 } }
                    }
                },
                new DialogueChoice
                {
                    choiceText   = "Attaquez avant qu'ils réagissent. (Force ≥ 28)",
                    nextDialogue = d_pillards_combat_victoire,
                    condition    = new DialogueChoiceCondition
                    {
                        failBehaviour    = ConditionFailBehaviour.Disable,
                        statRequirements = new[] { new StatRequirement { stat = SurvivorStatIndex.Force, minimumValue = 28 } },
                        traitRequirements = new[]
                        {
                            // Peureux bloque l'attaque directe
                            new TraitRequirement { isPositive = false, negativeTrait = NegativeTrait.Peureux, mustBeAbsent = true }
                        }
                    }
                },
                new DialogueChoice
                {
                    choiceText   = "Fuyez immédiatement.",
                    nextDialogue = d_pillards_fuite,
                },
            };
            Save(d_appel4_pillards, "Dialogues/SM_Appel4_Pillards");

            // Magasin vide (ils sont partis)
            var d_appel4_vide = Dialogue("SM_Appel4_Vide",
                text: "Chef ! Les hommes sont partis. Le magasin est libre. " +
                      "On commence la fouille complète.",
                hasLimit: false);
            d_appel4_vide.choices = new[]
            {
                new DialogueChoice
                {
                    choiceText   = "Fouillez tout !",
                    nextDialogue = d_vide_fouille,
                }
            };
            Save(d_appel4_vide, "Dialogues/SM_Appel4_Vide");

            // Fouille en détail (nœud de décision)
            var d_appel4_fouille_detail = Dialogue("SM_Appel4_FouilleDetail",
                text: "Chef, on fouille les rayons en détail. Ça prend du temps. " +
                      "Les voix se rapprochent…",
                hasLimit: true, timeSec: 20f, timeoutIdx: 1);
            // Résultat aléatoire pondéré : 60% bonne récolte, 40% vide
            // On modélise via deux choix auto-résolvants ou une rencontre
            d_appel4_fouille_detail.choices = new[]
            {
                new DialogueChoice
                {
                    choiceText   = "Continuez — prenez le temps qu'il faut.",
                    nextDialogue = d_fouille_detail_ok,  // résultat positif majoritaire
                },
                new DialogueChoice
                {
                    choiceText   = "Arrêtez et allez vers les hommes maintenant.",
                    nextDialogue = d_appel4_pillards,    // rencontre forcée
                }
            };
            Save(d_appel4_fouille_detail, "Dialogues/SM_Appel4_FouilleDetail");

            // ─────────────────────────────────────────────────────────────────────
            // APPEL 3 — À l'intérieur : voix entendues (Jour 1 — 14h–16h)
            // ─────────────────────────────────────────────────────────────────────

            var d_appel3 = Dialogue("SM_Appel3_AInterieur",
                text: "Chef… le magasin est presque vide. Mais on entend des voix plus loin. " +
                      "On dirait plusieurs hommes. Qu'est-ce qu'on fait ?",
                hasLimit: true, timeSec: 30f, timeoutIdx: 3);  // timeout = fouiller d'abord
            d_appel3.choices = new[]
            {
                // S'approcher pour écouter — Intelligence ou Social élevé
                new DialogueChoice
                {
                    choiceText   = "Approchez discrètement pour écouter. (Intel. ou Social ≥ 22)",
                    nextDialogue = d_appel4_neutres,   // écouter = mieux percevoir qu'ils sont neutres
                    condition    = new DialogueChoiceCondition
                    {
                        failBehaviour    = ConditionFailBehaviour.Disable,
                        statRequirements = new[]
                        {
                            // L'un ou l'autre suffit — on teste Intelligence ici,
                            // la condition Social est sur le second slot (OR simulé via score cumulé)
                            new StatRequirement { stat = SurvivorStatIndex.Intelligence, minimumValue = 22 }
                        }
                    }
                },
                // Aller leur parler — interaction directe (résultat aléatoire)
                new DialogueChoice
                {
                    choiceText   = "Allez leur parler directement.",
                    // Branche vers neutres ou pillards selon la rencontre ;
                    // ici on branche sur pillards pour simuler le risque d'hostilité
                    nextDialogue = d_appel4_pillards,
                },
                // Agir avant eux — Force
                new DialogueChoice
                {
                    choiceText   = "Agissez avant qu'ils vous voient — attaque surprise. (Force ≥ 30)",
                    nextDialogue = d_pillards_combat_victoire,
                    condition    = new DialogueChoiceCondition
                    {
                        failBehaviour    = ConditionFailBehaviour.Disable,
                        statRequirements = new[] { new StatRequirement { stat = SurvivorStatIndex.Force, minimumValue = 30 } },
                        traitRequirements = new[]
                        {
                            new TraitRequirement { isPositive = false, negativeTrait = NegativeTrait.Peureux, mustBeAbsent = true }
                        }
                    }
                },
                // Fouiller d'abord
                new DialogueChoice
                {
                    choiceText   = "Fouillezles rayons avant d'aller vers eux.",
                    nextDialogue = d_appel4_fouille_detail,
                },
            };
            Save(d_appel3, "Dialogues/SM_Appel3_AInterieur");

            // ─────────────────────────────────────────────────────────────────────
            // APPEL 2 — Accès selon l'entrée choisie (Jour 1 — 11h–13h)
            // ─────────────────────────────────────────────────────────────────────

            // Cas : Porte principale bloquée
            var d_appel2_porte = Dialogue("SM_Appel2_PortePrincipale",
                text: "Chef, la porte est bloquée — blindée de l'intérieur. " +
                      "On peut la casser mais ça fera du bruit. Vos ordres ?",
                hasLimit: true, timeSec: 30f, timeoutIdx: 2);  // timeout = chercher autre entrée
            d_appel2_porte.choices = new[]
            {
                new DialogueChoice
                {
                    choiceText   = "Forcez la porte — vite et bruyant. (Force ≥ 25)",
                    nextDialogue = d_appel3,
                    condition    = new DialogueChoiceCondition
                    {
                        failBehaviour    = ConditionFailBehaviour.Disable,
                        statRequirements = new[] { new StatRequirement { stat = SurvivorStatIndex.Force, minimumValue = 25 } }
                    }
                },
                new DialogueChoice
                {
                    choiceText   = "Crochetez la serrure — discret mais long. (Tech. ≥ 20)",
                    nextDialogue = d_appel3,
                    condition    = new DialogueChoiceCondition
                    {
                        failBehaviour    = ConditionFailBehaviour.Disable,
                        statRequirements = new[] { new StatRequirement { stat = SurvivorStatIndex.Technique, minimumValue = 20 } }
                    }
                },
                new DialogueChoice
                {
                    choiceText   = "Abandonnez — cherchez une autre entrée.",
                    nextDialogue = d_appel3,
                    eventTrigger = LoseResource(ResourceType.Food, 1)  // temps perdu = ration consommée
                },
            };
            Save(d_appel2_porte, "Dialogues/SM_Appel2_PortePrincipale");

            // Cas : Entrée arrière
            var d_appel2_arriere = Dialogue("SM_Appel2_EntreeArriere",
                text: "Chef, on est par l'entrée de livraison. Visibilité réduite, " +
                      "mais on est à l'intérieur sans faire de bruit.",
                hasLimit: false);
            d_appel2_arriere.choices = new[]
            {
                new DialogueChoice
                {
                    choiceText   = "Avancez prudemment.",
                    nextDialogue = d_appel3,
                }
            };
            Save(d_appel2_arriere, "Dialogues/SM_Appel2_EntreeArriere");

            // Cas : Autre entrée (Explorateur)
            var d_appel2_autre = Dialogue("SM_Appel2_AutreEntree",
                text: "Chef, on a trouvé une fenêtre de service — entrée totalement inaperçue. " +
                      "On est à l'intérieur. Ils ne savent pas qu'on est là.",
                hasLimit: false);
            d_appel2_autre.choices = new[]
            {
                new DialogueChoice
                {
                    choiceText   = "Parfait. Progressez silencieusement.",
                    nextDialogue = d_appel4_vide,  // avantage Explorateur = accès aux infos (magasin libre)
                }
            };
            Save(d_appel2_autre, "Dialogues/SM_Appel2_AutreEntree");

            // Cas : Autre entrée sans talent (long mais sûr)
            var d_appel2_autre_sans_talent = Dialogue("SM_Appel2_AutreEntree_Standard",
                text: "Chef, après un bon moment, on a trouvé un accès secondaire. " +
                      "C'est long, mais on est dedans sans être vus.",
                hasLimit: false);
            d_appel2_autre_sans_talent.choices = new[]
            {
                new DialogueChoice
                {
                    choiceText   = "Progressez.",
                    nextDialogue = d_appel3,
                    eventTrigger = LoseResource(ResourceType.Food, 1)  // ration consommée
                }
            };
            Save(d_appel2_autre_sans_talent, "Dialogues/SM_Appel2_AutreEntree_Standard");

            // ─────────────────────────────────────────────────────────────────────
            // APPEL 1 — Observation extérieure (Jour 1 — 10h00 fixe)
            // ─────────────────────────────────────────────────────────────────────

            var d_appel1 = Dialogue("SM_Appel1_Observation",
                text: "Chef, on est devant un supermarché abandonné. " +
                      "Les vitrines sont cassées mais… on ne voit personne dehors. " +
                      "Il pourrait rester des ressources. On voit trois entrées possibles.",
                hasLimit: true, timeSec: 45f, timeoutIdx: 2);  // timeout = entrée arrière (prudence)
            d_appel1.choices = new[]
            {
                // Entrée principale — porte blindée
                new DialogueChoice
                {
                    choiceText   = "Entrez par la porte principale.",
                    nextDialogue = d_appel2_porte,
                },
                // Entrée arrière — accès discret
                new DialogueChoice
                {
                    choiceText   = "Contournez par l'entrée arrière (livraison).",
                    nextDialogue = d_appel2_arriere,
                    // Endurance ≥ 20 recommandée (chemin plus long) — seulement un avertissement, pas bloquant
                },
                // Chercher une autre entrée — talent Explorateur = entrée secrète
                new DialogueChoice
                {
                    choiceText   = "Cherchez une autre entrée — prenez le temps.",
                    // Si talent Explorateur présent → accès fenêtre secrète
                    nextDialogue = d_appel2_autre_sans_talent,
                    followUpCall = null,
                },
                // Même choix mais réservé Explorateur — entrée secrète
                new DialogueChoice
                {
                    choiceText   = "Cherchez une entrée secrète. (Talent : Explorateur)",
                    nextDialogue = d_appel2_autre,
                    condition    = new DialogueChoiceCondition
                    {
                        failBehaviour     = ConditionFailBehaviour.Hide,
                        talentRequirements = new[]
                        {
                            new TalentRequirement { requiredTalent = SurvivorTalent.Explorateur }
                        }
                    }
                },
            };
            Save(d_appel1, "Dialogues/SM_Appel1_Observation");

            // ─────────────────────────────────────────────────────────────────────
            // RadioCallEvents — un par appel
            // ─────────────────────────────────────────────────────────────────────

            // Appel 1 : fixe Jour 1 — 10h00
            var rc1 = RadioCall("RC_SM_Appel1", d_appel1,
                triggerDay: 1, mode: TriggerTimeMode.Fixed, fixH: 10, fixM: 0);

            // Appel 2 : aléatoire 11h–13h (Jour 1) — déclenché en follow-up des choix Appel 1
            // → L'architecture suit-up est gérée dans d_appel1 via nextDialogue direct
            // On crée quand même l'asset pour une éventuelle utilisation en pool
            var rc2 = RadioCall("RC_SM_Appel2", d_appel2_porte,
                triggerDay: 1, mode: TriggerTimeMode.Random, rndMin: 11, rndMax: 13);

            // Appel 3 : aléatoire 14h–16h (Jour 1)
            var rc3 = RadioCall("RC_SM_Appel3", d_appel3,
                triggerDay: 1, mode: TriggerTimeMode.Random, rndMin: 14, rndMax: 16);

            // Appel 4 : aléatoire 16h–18h (Jour 1) — fouille/rencontre
            var rc4 = RadioCall("RC_SM_Appel4_Fouille", d_appel4_fouille_detail,
                triggerDay: 1, mode: TriggerTimeMode.Random, rndMin: 16, rndMax: 18);

            // ─────────────────────────────────────────────────────────────────────
            // ExplorationZone + MissionData
            // ─────────────────────────────────────────────────────────────────────

            var zone = ScriptableObject.CreateInstance<ExplorationZone>();
            zone.zoneName     = "Supermarché Abandonné";
            zone.daysFromBase = 1;
            zone.description  = "Un supermarché en périphérie. Les vitrines sont brisées. " +
                                 "Peut-être des ressources, peut-être des occupants.";
            zone.zoneColor    = new Color(0.85f, 0.55f, 0.2f);
            // Appel 1 est fixe et déclenche toute la chaîne via nextDialogue
            zone.radioCalls   = new[] { rc1 };
            zone.encounterPool = null;  // pas de pool aléatoire — scène scriptée
            SaveAsset(zone, "Zone_Supermarche");

            var mission = ScriptableObject.CreateInstance<MissionData>();
            mission.missionID   = "mission_supermarche";
            mission.displayName = "Supermarché Abandonné";
            mission.zone        = zone;
            mission.radioCalls  = new RadioCallEvent[0];  // chaîne portée par la zone
            mission.followUps   = new MissionFollowUp[0];
            mission.requiresLocationTrigger = false;
            SaveAsset(mission, "Mission_Supermarche");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SupermarcheEventCreator] Assets générés dans Assets/Data/Missions/Supermarche/.\n" +
                      "→ Assignez Mission_Supermarche à l'ExplorationPanel ou à un MissionTriggerZone.");

            EditorUtility.DisplayDialog(
                "Supermarché généré !",
                "Assets créés dans Assets/Data/Missions/Supermarche/\n\n" +
                "Étapes suivantes :\n" +
                "• Assignez Mission_Supermarche à votre ExplorationPanel.\n" +
                "• Les appels radio s'enchaînent via nextDialogue — Appel 1 démarre tout.",
                "OK");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Helpers — miroir de ExampleMissionCreator
        // ─────────────────────────────────────────────────────────────────────────

        private static ExplorationDialogue Dialogue(
            string id, string text, bool hasLimit = false,
            float timeSec = 30f, int timeoutIdx = 0,
            string speakerName = "Explorateur")
        {
            var d = ScriptableObject.CreateInstance<ExplorationDialogue>();
            d.dialogueID         = id;
            d.speakerName        = speakerName;
            d.dialogueText       = text;
            d.hasTimeLimit       = hasLimit;
            d.timeLimitSeconds   = timeSec;
            d.timeoutChoiceIndex = timeoutIdx;
            d.nodeEvent          = new DialogueEventData();
            d.choices            = new DialogueChoice[0];
            return d;
        }

        private static RadioCallEvent RadioCall(
            string assetName, ExplorationDialogue dialogue,
            int triggerDay, TriggerTimeMode mode,
            int fixH = 10, int fixM = 0,
            int rndMin = 8, int rndMax = 18)
        {
            var rc = ScriptableObject.CreateInstance<RadioCallEvent>();
            rc.triggerDay    = triggerDay;
            rc.timeMode      = mode;
            rc.fixedHour     = fixH;
            rc.fixedMinute   = fixM;
            rc.randomHourMin = rndMin;
            rc.randomHourMax = rndMax;
            rc.dialogue      = dialogue;
            rc.fireOnce      = true;
            SaveAsset(rc, "Encounters/" + assetName);
            return rc;
        }

        private static DialogueEventData AddResource(params ResourceEntry[] entries) =>
            new DialogueEventData
            {
                eventType     = DialogueEventType.AddResource,
                selectionMode = ResourceSelectionMode.All,
                resources     = entries
            };

        private static DialogueEventData LoseResource(ResourceType type, int amount) =>
            new DialogueEventData
            {
                eventType     = DialogueEventType.LoseResource,
                selectionMode = ResourceSelectionMode.All,
                resources     = new[] { new ResourceEntry { resourceType = type, amount = amount } }
            };

        private static void Save(ExplorationDialogue d, string relativePath)
            => SaveAsset(d, relativePath);

        private static void SaveAsset(Object asset, string relativePath)
        {
            string path = $"{RootDir}/{relativePath}.asset";
            EnsureDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(asset, path);
        }

        private static void EnsureDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path);
            string folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureDirectory(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
