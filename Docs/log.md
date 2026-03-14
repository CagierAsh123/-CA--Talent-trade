Could not load reference to Verse.ThingDef named Human

UnityEngine.StackTraceUtility:ExtractStackTrace ()

Verse.Log:Error (string)

Verse.ScribeExtractor:DefFromNode<Verse.ThingDef> (System.Xml.XmlNode)

Verse.Scribe\_Defs:Look<Verse.ThingDef> (Verse.ThingDef\&,string)

(wrapper dynamic-method) MonoMod.Utils.DynamicMethodDefinition:Verse.Thing.ExposeData\_Patch1 (Verse.Thing)

Verse.ThingWithComps:ExposeData ()

Verse.Pawn:ExposeData ()

Verse.ScribeExtractor:SaveableFromNode<Verse.Pawn> (System.Xml.XmlNode,object\[])

TalentTrade.PawnDeserializer:XmlToPawn (string)

TalentTrade.PawnDeserializer:Deserialize (string)

TalentTrade.PawnDeserializer:DeserializeAndSpawn (string,Verse.Map)

TalentTrade.TalentTradeManager/<>c\_\_DisplayClass44\_0:<HandleMarketSell>b\_\_0 ()

TalentTrade.TalentTradeManager:RunMainThreadQueue ()

TalentTrade.TalentTradeManager:Update ()

TalentTrade.Patches.RootUpdatePatches:Postfix ()

(wrapper dynamic-method) MonoMod.Utils.DynamicMethodDefinition:Verse.Root.Update\_Patch4 (Verse.Root)

Verse.Root\_Entry:Update ()



SaveableFromNode exception: System.NullReferenceException: Object reference not set to an instance of an object

\[Ref FAEAEE91]

&nbsp; at Verse.Thing.ExposeData () \[0x00034] in <46372f5dadbf4af8939e608076251180>:0 

&nbsp;   - TRANSPILER bs.savegamecompatibilityoperation: IEnumerable`1 SaveGameCompatibility.ExposeDataPatch:Transpiler(IEnumerable`1 instructions, MethodBase targetMethod)

&nbsp; at Verse.ThingWithComps.ExposeData () \[0x00000] in <46372f5dadbf4af8939e608076251180>:0 

&nbsp; at Verse.Pawn.ExposeData () \[0x00000] in <46372f5dadbf4af8939e608076251180>:0 

&nbsp; at Verse.ScribeExtractor.SaveableFromNode\[T] (System.Xml.XmlNode subNode, System.Object\[] ctorArgs) \[0x001dd] in <46372f5dadbf4af8939e608076251180>:0 

Subnode:

<saveable Class="Pawn"><def>Human</def><tickDelta>9</tickDelta><id>Human428</id><map>0</map><pos>(118, 0, 130)</pos><rot>3</rot><faction>Faction\_37</faction><questTags IsNull="True" /><spawnedTick>0</spawnedTick><despawnedTick>-1</despawnedTick><beenRevealed>True</beenRevealed><selectedArtifact>null</selectedArtifact><mostSevere>null</mostSevere><OriginMap>null</OriginMap><spawnedOnMapEver><li>Map\_0</li></spawnedOnMapEver><targetHolder>null</targetHolder><lastStudiedTick>-9999999</lastStudiedTick><HeadControllerComp\_faceType>HeadSquare</HeadControllerComp\_faceType><HeadControllerComp\_color>RGBA(0.949, 0.780, 0.549, 1.000)</HeadControllerComp\_color><EyeballControllerComp\_faceType>EyeWide</EyeballControllerComp\_faceType><EyeballControllerComp\_color>RGBA(0.455, 0.388, 0.329, 1.000)</EyeballControllerComp\_color><LidControllerComp\_faceType>LidNormal</LidControllerComp\_faceType><LidControllerComp\_color>RGBA(0.298, 0.269, 0.250, 1.000)</LidControllerComp\_color><BrowControllerComp\_faceType>BrowNormal</BrowControllerComp\_faceType><BrowControllerComp\_color>RGBA(0.298, 0.269, 0.250, 1.000)</BrowControllerComp\_color><MouthControllerComp\_faceType>MouthNormal</MouthControllerComp\_faceType><MouthControllerComp\_color>RGBA(0.949, 0.780, 0.549, 1.000)</MouthControllerComp\_color><SkinControllerComp\_faceType>SkinNormal</SkinControllerComp\_faceType><SkinControllerComp\_color>RGBA(0.949, 0.780, 0.549, 1.000)</SkinControllerComp\_color><activedGenes IsNull="True" /><AlienRaces\_AlienComp><addonVariants /><addonColors /><colorChannels><keys><li>base</li><li>hair</li><li>skin</li><li>skinBase</li><li>tattoo</li><li>favorite</li><li>ideo</li><li>mech</li></keys><values><li><first>RGBA(1.000, 1.000, 1.000, 1.000)</first><second>RGBA(1.000, 1.000, 1.000, 1.000)</second></li><li><first>RGBA(0.298, 0.269, 0.250, 1.000)</first></li><li><first>RGBA(0.949, 0.780, 0.549, 1.000)</first></li><li><first>RGBA(0.949, 0.780, 0.549, 1.000)</first></li><li><first>RGBA(0.949, 0.780, 0.549, 0.800)</first></li><li><first>RGBA(0.325, 0.580, 0.263, 1.000)</first><second>RGBA(0.325, 0.580, 0.263, 1.000)</second></li><li><first>RGBA(0.000, 0.737, 0.847, 1.000)</first><second>RGBA(0.412, 0.770, 0.824, 1.000)</second></li><li><first>RGBA(0.200, 0.840, 0.840, 1.000)</first></li></values></colorChannels><colorChannelLinks><keys /><values /></colorChannelLinks><headVariant>0</headVariant><bodyVariant>0</bodyVariant><headMaskVariant>0</headMaskVariant><bodyMaskVariant>0</bodyMaskVariant></AlienRaces\_AlienComp><ticksToReset>2147483483</ticksToReset><lastKeepDisplayTick>-9999</lastKeepDisplayTick><learnedAbilities /><currentlyCasting>null</currentlyCasting><currentlyCastingTargets /><cachedPawnBeauty>33</cachedPawnBeauty><activeMemories /><situationalMemories /><eventLogMemories /><archiveMemories /><kindDef>Colonist</kindDef><name Class="NameTriple"><first>Lynn</first><nick>人1</nick><last>Romero</last></name><deadlifeDustFaction>null</deadlifeDustFaction><mindState><meleeThreat>null</meleeThreat><enemyTarget>null</enemyTarget><knownExploder>null</knownExploder><lastMannedThing>null</lastMannedThing><droppedWeapon>null</droppedWeapon><lastAttackedTarget>(0, 0, 0)</lastAttackedTarget><thinkData><keys /><values /></thinkData><lastJobTag>Idle</lastJobTag><nextApparelOptimizeTick>7803</nextApparelOptimizeTick><lastEngageTargetTick>-99999</lastEngageTargetTick><lastAttackTargetTick>-99999</lastAttackTargetTick><canFleeIndividual>True</canFleeIndividual><lastMeleeThreatHarmTick>-99999</lastMeleeThreatHarmTick><nextMoveOrderIsWait>False</nextMoveOrderIsWait><duty IsNull="True" /><mentalStateHandler><curState IsNull="True" /></mentalStateHandler><mentalBreaker /><mentalFitGenerator /><inspirationHandler><curState IsNull="True" /></inspirationHandler><priorityWork><prioritizedCell>(-1000, -1000, -1000)</prioritizedCell></priorityWork><lastSelfTendTick>-99999</lastSelfTendTick><breachingTarget IsNull="True" /><babyAutoBreastfeedMoms><keys /><values /></babyAutoBreastfeedMoms><babyCaravanBreastfeed><keys /><values /></babyCaravanBreastfeed><resurrectTarget IsNull="True" /><lastRangedHarmTick>0</lastRangedHarmTick><lastDayInteractionTick>15155</lastDayInteractionTick></mindState><jobs><curJob><commTarget>null</commTarget><verbToUse>null</verbToUse><bill>null</bill><lord>null</lord><quest>null</quest><def>Wait\_Wander</def><loadID>71</loadID><targetQueueA IsNull="True" /><targetQueueB IsNull="True" /><countQueue IsNull="True" /><startTick>115</startTick><expiryInterval>158</expiryInterval><placedThings IsNull="True" /><jobGiverThinkTree>Humanlike</jobGiverThinkTree><psyfocusTargetLast>-1</psyfocusTargetLast><ability>null</ability><source>null</source><interactableIndex>-1</interactableIndex><lastJobGiverKey>1317176504</lastJobGiverKey></curJob><curDriver Class="JobDriver\_Wait"><curToilIndex>0</curToilIndex><ticksLeftThisToil>-49</ticksLeftThisToil><startTick>115</startTick><locomotionUrgencySameAs>null</locomotionUrgencySameAs></curDriver><jobQueue><jobs /></jobQueue><formingCaravanTick>-1</formingCaravanTick></jobs><stances><stunner><showStunMote>True</showStunMote><adaptationTicksLeft><keys /><values /></adaptationTicksLeft></stunner><stagger /><curStance Class="Stance\_Mobile" /></stances><infectionVectors><givenPrearrival>True</givenPrearrival><pathways><keys /><values /></pathways></infectionVectors><verbTracker><verbs><li Class="Verb\_MeleeAttackDamage"><loadID>Thing\_Human428\_0\_Smash</loadID><currentTarget>(0, 0, 0)</currentTarget><currentDestination>(0, 0, 0)</currentDestination><lastShotTick>-999999</lastShotTick><canHitNonTargetPawnsNow>True</canHitNonTargetPawnsNow></li><li Class="Verb\_MeleeAttackDamage"><loadID>Thing\_Human428\_1\_Smash</loadID><currentTarget>(0, 0, 0)</currentTarget><currentDestination>(0, 0, 0)</currentDestination><lastShotTick>-999999</lastShotTick><canHitNonTargetPawnsNow>True</canHitNonTargetPawnsNow></li><li Class="Verb\_MeleeAttackDamage"><loadID>Thing\_Human428\_2\_Bite</loadID><currentTarget>(0, 0, 0)</currentTarget><currentDestination>(0, 0, 0)</currentDestination><lastShotTick>-999999</lastShotTick><canHitNonTargetPawnsNow>True</canHitNonTargetPawnsNow></li><li Class="Verb\_MeleeAttackDamage"><loadID>Thing\_Human428\_3\_Smash</loadID><currentTarget>(0, 0, 0)</currentTarget><currentDestination>(0, 0, 0)</currentDestination><lastShotTick>-999999</lastShotTick><canHitNonTargetPawnsNow>True</canHitNonTargetPawnsNow></li></verbs></verbTracker><natives><verbTracker><verbs IsNull="True" /></verbTracker></natives><meleeVerbs><curMeleeVerb>Verb\_Thing\_Human428\_1\_Smash</curMeleeVerb><curMeleeVerbUpdateTick>105</curMeleeVerbUpdateTick><terrainVerbs IsNull="True" /></meleeVerbs><rotationTracker /><pather><moving>False</moving><nextCell>(118, 0, 130)</nextCell><nextCellCostInitial>1</nextCellCostInitial><peMode>OnCell</peMode><cellsUntilClamor>11</cellsUntilClamor><lastEnteredCellTick>106</lastEnteredCellTick><lastMovedTick>106</lastMovedTick></pather><carryTracker><innerContainer><maxStacks>1</maxStacks><innerList /></innerContainer></carryTracker><apparel><wornApparel><innerList><li><def>Apparel\_Pants</def><id>Apparel\_Pants429</id><health>130</health><stackCount>1</stackCount><stuff>Synthread</stuff><questTags IsNull="True" /><despawnedTick>-1</despawnedTick><quality>Normal</quality><sourcePrecept>null</sourcePrecept><everSeenByPlayer>True</everSeenByPlayer><abilities /></li><li><def>Apparel\_CollarShirt</def><id>Apparel\_CollarShirt430</id><health>130</health><stackCount>1</stackCount><stuff>Synthread</stuff><questTags IsNull="True" /><despawnedTick>-1</despawnedTick><quality>Normal</quality><sourcePrecept>null</sourcePrecept><everSeenByPlayer>True</everSeenByPlayer><abilities /></li></innerList></wornApparel><lockedApparel IsNull="True" /><lastApparelWearoutTick>35</lastApparelWearoutTick></apparel><story><bodyType>Male</bodyType><hairDef>AFUhairM12</hairDef><hairColor>RGBA(0.298, 0.269, 0.250, 1.000)</hairColor><traits><allTraits><li><def>Immunity</def><sourceGene>null</sourceGene><degree>-1</degree><suppressedBy>null</suppressedBy></li><li><def>Kiiro\_ShoppingTendency</def><sourceGene>null</sourceGene><degree>-1</degree><suppressedBy>null</suppressedBy></li></allTraits></traits><birthLastName>Romero</birthLastName><favoriteColorDef>DarkGreen</favoriteColorDef><headType>Male\_AverageNormal</headType><childhood>SicklyLiar3</childhood></story><equipment><equipment><innerList /></equipment><bondedWeapon>null</bondedWeapon></equipment><drafter><autoUndrafter /></drafter><ageTracker><ageBiologicalTicks>51933762</ageBiologicalTicks><birthAbsTicks>-98665820</birthAbsTicks><growth>0.80144012</growth><progressToNextBiologicalTick>0.991054535</progressToNextBiologicalTick><nextGrowthCheckTick>240</nextGrowthCheckTick><ageReversalDemandedAtAgeTicks>92040000</ageReversalDemandedAtAgeTicks></ageTracker><healthTracker><hediffSet><hediffs><li Class="RimSkyBlock.Hediff\_EmpireEffects"><loadID>220</loadID><ageTicks>155</ageTicks><visible>True</visible><severity>0.5</severity><canBeThreateningToPart>True</canBeThreateningToPart><def>RSB\_EmpireEffects</def><combatLogEntry>null</combatLogEntry><abilities IsNull="True" /></li><li Class="RimTalk.Data.Hediff\_Persona"><loadID>434</loadID><ageTicks>155</ageTicks><severity>0.5</severity><canBeThreateningToPart>True</canBeThreateningToPart><def>RimTalk\_PersonaData</def><combatLogEntry>null</combatLogEntry><abilities IsNull="True" /><Personality>辣妹/现充 - 极其随意，使用大量的流行语、缩写和俚语。语气轻浮，注重当下的快乐。</Personality><TalkInitiationWeight>0.600000024</TalkInitiationWeight><SpokenThoughtTicks><keys><li>ClothedNudist\_0</li><li>Expectations\_5</li><li>TreesDesired\_14</li><li>TreeDensityReduced\_0</li><li>DesireForProtection\_4</li></keys><values><li>30</li><li>30</li><li>30</li><li>30</li><li>108</li></values></SpokenThoughtTicks></li><li Class="HediffWithComps"><loadID>437</loadID><ageTicks>155</ageTicks><tickAdded>1</tickAdded><visible>True</visible><severity>0.5</severity><canBeThreateningToPart>True</canBeThreateningToPart><def>PYF\_RelationshipBuff</def><combatLogEntry>null</combatLogEntry><abilities IsNull="True" /></li></hediffs></hediffSet><surgeryBills><bills /></surgeryBills><immunity><imList /></immunity></healthTracker><records><records><vals><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>160</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li></vals></records><battleActive>null</battleActive></records><inventory><itemsNotForSale /><unpackedCaravanItems /><innerContainer><innerList /></innerContainer></inventory><filth><carriedFilth /></filth><roping><hitchingPostInt>null</hitchingPostInt><ropees /></roping><needs><needs><li Class="Need\_Mood"><def>Mood</def><curLevel>0.507200003</curLevel><thoughts><memories><memories><li><def>NewColonyOptimism</def><sourcePrecept>null</sourcePrecept><otherPawn>null</otherPawn><age>150</age><durationTicksOverride>480000</durationTicksOverride></li></memories></memories></thoughts><recentMemory><lastLightTick>135</lastLightTick><lastOutdoorTick>135</lastOutdoorTick></recentMemory></li><li Class="Need\_Food"><def>Food</def><curLevel>0.796000004</curLevel><lastNonStarvingTick>135</lastNonStarvingTick></li><li Class="Need\_Rest"><def>Rest</def><curLevel>0.988408685</curLevel></li><li Class="Need\_Joy"><def>Joy</def><curLevel>0.538534641</curLevel><tolerances><vals><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li></vals></tolerances><bored><vals><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li><li>False</li></vals></bored></li><li Class="Need\_Beauty"><def>Beauty</def><curLevel>0.495200008</curLevel></li><li Class="Need\_Comfort"><def>Comfort</def><curLevel>0.497599989</curLevel></li><li Class="Need\_Chemical\_Any"><def>DrugDesire</def><curLevel>0.5</curLevel></li><li Class="Need\_RoomSize"><def>RoomSize</def><curLevel>0.55400002</curLevel></li><li Class="RomanceOnTheRim.Need\_Romance"><lastLordStartTick>-99999</lastLordStartTick><lastLordCheckTick>-99999</lastLordCheckTick><isWorryingAbout>null</isWorryingAbout><separatedPawns IsNull="True" /><separateTick IsNull="True" /><relationRecords IsNull="True" /><def>RomanceOnTheRim\_Need\_Romance</def><curLevel>0.5</curLevel></li><li Class="Need\_Indoors"><def>Indoors</def><curLevel>0.999374986</curLevel></li></needs></needs><guest><hostFaction>null</hostFaction><slaveFaction>null</slaveFaction><joinStatus>JoinAsSlave</joinStatus><interactionMode>MaintainOnly</interactionMode><slaveInteractionMode>NoInteraction</slaveInteractionMode><spotToWaitInsteadOfEscaping>(-1000, -1000, -1000)</spotToWaitInsteadOfEscaping><lastPrisonBreakTicks>-1</lastPrisonBreakTicks><ideoForConversion>null</ideoForConversion><enabledNonExclusiveInteractions /><lastResistanceInteractionData IsNull="True" /><finalResistanceInteractionData IsNull="True" /></guest><guilt /><royalty><titles /><favor><keys /><values /></favor><highestTitles><keys /><values /></highestTitles><heirs><keys /><values /></heirs><permits /><abilities /></royalty><social><directRelations /><virtualRelations /><relativeInvolvedInRescueQuest>null</relativeInvolvedInRescueQuest><pregnancyApproaches><keys /><values /></pregnancyApproaches><romanceEnableTick>-1</romanceEnableTick><additionalPregnancyApproachData><partners><keys /><values /></partners></additionalPregnancyApproachData></social><psychicEntropy><limitEntropyAmount>True</limitEntropyAmount></psychicEntropy><shambler IsNull="True" /><ownership><ownedBed>null</ownedBed><assignedMeditationSpot>null</assignedMeditationSpot><assignedGrave>null</assignedGrave><assignedThrone>null</assignedThrone><assignedDeathrestCasket>null</assignedDeathrestCasket></ownership><interactions /><skills><skills><li><def>Shooting</def></li><li><def>Melee</def><level>1</level></li><li><def>Construction</def><level>1</level></li><li><def>Mining</def><level>2</level><passion>Minor</passion></li><li><def>Cooking</def><level>2</level><passion>Minor</passion></li><li><def>Plants</def></li><li><def>Animals</def><level>2</level><passion>Minor</passion></li><li><def>Crafting</def><level>3</level><passion>Minor</passion></li><li><def>Artistic</def><level>2</level></li><li><def>Medicine</def><level>5</level></li><li><def>Social</def><level>10</level><passion>Major</passion></li><li><def>Intellectual</def></li></skills><lastXpSinceMidnightResetTimestamp>-1</lastXpSinceMidnightResetTimestamp></skills><abilities><abilities /></abilities><ideo><ideo>Ideo\_28</ideo><previousIdeos /><certainty>0.949571669</certainty><babyIdeoExposure IsNull="True" /></ideo><workSettings><priorities><vals><li>3</li><li>3</li><li>0</li><li>3</li><li>3</li><li>3</li><li>0</li><li>3</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>0</li><li>3</li><li>3</li><li>0</li><li>0</li><li>0</li><li>0</li><li>3</li><li>3</li><li>0</li><li>0</li><li>3</li><li>3</li><li>3</li></vals></priorities></workSettings><trader IsNull="True" /><outfits><curOutfit>ApparelPolicy\_任意\_1</curOutfit><overrideHandler><forcedAps /></overrideHandler></outfits><drugs><curAssignedDrugs>DrugPolicy\_社交成瘾品\_1</curAssignedDrugs><drugTakeRecords /></drugs><foodRestriction><curRestriction>FoodPolicy\_无限制\_1</curRestriction><allowedBabyFoodTypes IsNull="True" /></foodRestriction><timetable><times><li>Sleep</li><li>Sleep</li><li>Sleep</li><li>Sleep</li><li>Sleep</li><li>Sleep</li><li>Anything</li><li>Anything</li><li>Anything</li><li>Anything</li><li>Anything</li><li>Anything</li><li>Anything</li><li>Anything</li><li>Anything</li><li>Anything</li><li>Anything</li><li>Anything</li><li>Anything</li><li>Anything</li><li>Joy</li><li>Joy</li><li>Sleep</li><li>Sleep</li></times></timetable><playerSettings><medCare>Best</medCare><allowedAreas><keys /><values /></allowedAreas><master>null</master><hostilityResponse>Attack</hostilityResponse><displayOrder>1</displayOrder></playerSettings><training IsNull="True" /><style><beardDef>NoBeard</beardDef><faceTattoo>NoTattoo\_Face</faceTattoo><bodyTattoo>NoTattoo\_Body</bodyTattoo></style><styleObserver /><connections><connectedThings /></connections><inventoryStock><stockEntries><keys><li>Medicine</li></keys><values><li><thingDef>MedicineIndustrial</thingDef></li></values></stockEntries></inventoryStock><treeSightings><miniTreeSightings /><fullTreeSightings /><superTreeSightings /></treeSightings><thinker /><mechanitor IsNull="True" /><genes><xenogenes /><endogenes><li><def>Skin\_Melanin6</def><pawn>Thing\_Human428</pawn><overriddenByGene>null</overriddenByGene><loadID>459</loadID></li><li><def>Hair\_MidBlack</def><pawn>Thing\_Human428</pawn><overriddenByGene>null</overriddenByGene><loadID>460</loadID></li></endogenes><xenotype>Baseliner</xenotype></genes><learning IsNull="True" /><reading><curAssignment>null</curAssignment></reading><creepjoiner IsNull="True" /><duplicate /><flight /></saveable>

UnityEngine.StackTraceUtility:ExtractStackTrace ()

Verse.Log:Error (string)

Verse.ScribeExtractor:SaveableFromNode<Verse.Pawn> (System.Xml.XmlNode,object\[])

TalentTrade.PawnDeserializer:XmlToPawn (string)

TalentTrade.PawnDeserializer:Deserialize (string)

TalentTrade.PawnDeserializer:DeserializeAndSpawn (string,Verse.Map)

TalentTrade.TalentTradeManager/<>c\_\_DisplayClass44\_0:<HandleMarketSell>b\_\_0 ()

TalentTrade.TalentTradeManager:RunMainThreadQueue ()

TalentTrade.TalentTradeManager:Update ()

TalentTrade.Patches.RootUpdatePatches:Postfix ()

(wrapper dynamic-method) MonoMod.Utils.DynamicMethodDefinition:Verse.Root.Update\_Patch4 (Verse.Root)

Verse.Root\_Entry:Update ()



Called FinalizeLoading() but current mode is Inactive

UnityEngine.StackTraceUtility:ExtractStackTrace ()

Verse.Log:Error (string)

Verse.ScribeLoader:FinalizeLoading ()

TalentTrade.PawnDeserializer:XmlToPawn (string)

TalentTrade.PawnDeserializer:Deserialize (string)

TalentTrade.PawnDeserializer:DeserializeAndSpawn (string,Verse.Map)

TalentTrade.TalentTradeManager/<>c\_\_DisplayClass44\_0:<HandleMarketSell>b\_\_0 ()

TalentTrade.TalentTradeManager:RunMainThreadQueue ()

TalentTrade.TalentTradeManager:Update ()

TalentTrade.Patches.RootUpdatePatches:Postfix ()

(wrapper dynamic-method) MonoMod.Utils.DynamicMethodDefinition:Verse.Root.Update\_Patch4 (Verse.Root)

Verse.Root\_Entry:Update ()



【三角洲贸易】HandleMarketSell: Failed to deserialize pawn for listing 91bc0ccad1e3

UnityEngine.StackTraceUtility:ExtractStackTrace ()

Verse.Log:Error (string)

TalentTrade.TalentTradeManager/<>c\_\_DisplayClass44\_0:<HandleMarketSell>b\_\_0 ()

TalentTrade.TalentTradeManager:RunMainThreadQueue ()

TalentTrade.TalentTradeManager:Update ()

TalentTrade.Patches.RootUpdatePatches:Postfix ()

(wrapper dynamic-method) MonoMod.Utils.DynamicMethodDefinition:Verse.Root.Update\_Patch4 (Verse.Root)

Verse.Root\_Entry:Update ()





