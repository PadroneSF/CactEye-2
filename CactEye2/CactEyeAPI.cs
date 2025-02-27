
/********************************************************************
 * Formerly named CactEyeVars.cs.
 * Renamed to CactEyeAPI.cs.
 * 
 * The following is what forms the CactEye API. It remains largely 
 * unchanged from what it was in the original mod, though some 
 * objects may have been renamed to help with self-documentation.
 * 
 * Most of the functions contained within are related to the mod's
 * occultation experiment, though there are some other useful functions
 * as well.
********************************************************************/
using SpaceTuxUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CactEye2.InitialSetup;


namespace CactEye2
{
    /* ************************************************************************************************
    * Class Name: CactEyeAPI_Flight
    * Purpose: This will allow CactEye's Occultation Science Event Window to run in multiple specific scenes. 
    * Shamelessly stolen from Starstrider42, who shamelessly stole it from Trigger Au.
    * Thank you!
    * ************************************************************************************************/
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    internal class CactEyeAPI_Flight : CactEyeAPI
    {
    }

    /* ************************************************************************************************
    * Class Name: CactEyeAPI_SpaceCentre
    * Purpose: This will allow CactEye's CactEye's Occultation Science Event Window to run in multiple specific scenes. 
    * Shamelessly stolen from Starstrider42, who shamelessly stole it from Trigger Au.
    * Thank you!
    * ************************************************************************************************/
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    internal class CactEyeAPI_SpaceCentre : CactEyeAPI
    {
    }

    /* ************************************************************************************************
    * Class Name: CactEyeAPI_SpaceCentre
    * Purpose: This will allow CactEye's CactEye's Occultation Science Event Window to run in multiple specific scenes. 
    * Shamelessly stolen from Starstrider42, who shamelessly stole it from Trigger Au.
    * Thank you!
    * ************************************************************************************************/
    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    internal class CactEyeAPI_TrackingStation
    {
    }

    class CactEyeAPI : MonoBehaviour
    {



        public static Dictionary<CelestialBody, BodyInfo> bodyInfo = new Dictionary<CelestialBody, BodyInfo>();

        public static Dictionary<CelestialBody, List<CelestialBody>> bodyChildren = new Dictionary<CelestialBody, List<CelestialBody>>();

        public static Dictionary<CelestialBody, OccultationRecord> occExpAstCometDict = new Dictionary<CelestialBody, OccultationRecord>();

        public static bool KASavailable = false;

        private bool DoOnce = true;

        public void Start()
        {
            Log.Info("CactEyeAPI.Start");
            KACWrapper.InitKACWrapper();
            KASavailable = HasMod.hasMod("KAS");

            //root = KSPUtil.ApplicationRootPath.Replace("\\", "/");

            InitDataLists();
            GameEvents.onGameStatePostLoad.Add(RunAfterLoad);
        }

        public void OnDestroy()
        {
            GameEvents.onGameStatePostLoad.Remove(RunAfterLoad);
        }

        void RunAfterLoad(ConfigNode node)
        {
            Log.Info("RunAfterLoad");
            InitDataLists(true);
        }
        void InitDataLists(bool clear = false)
        {
            Log.Info("InitDataList, clear: " + clear);
            if (clear)
            {
                bodyChildren.Clear();
                occExpAstCometDict.Clear();
            }
            // Add all celestial bodies (except the Sun) to the bodyChildren dictionary, and their moons to those planets

            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body != Planetarium.fetch.Sun)
                {
                    if (!bodyChildren.ContainsKey(body.referenceBody))
                        bodyChildren.Add(body.referenceBody, new List<CelestialBody>());

                    bodyChildren[body.referenceBody].Add(body);

                }
                else
                    if (!bodyChildren.ContainsKey(body))
                    bodyChildren.Add(body, new List<CelestialBody>());
            }

#if true
            // Search all vessels for occultation experiments
            Log.Info("occultation: Searching all vessels for current occultation experiment data...");

            if (HighLogic.CurrentGame.flightState != null && HighLogic.CurrentGame.flightState.protoVessels == null)
                Log.Error("HighLogic.CurrentGame.flightState.protoVessels is null");
            if (HighLogic.CurrentGame.flightState != null)
            {
                foreach (ProtoVessel v in HighLogic.CurrentGame.flightState.protoVessels)
                {
                    Log.Info("occultation: CHECKING VESSEL " + v.vesselName);

                    foreach (ProtoPartSnapshot p in v.protoPartSnapshots)
                    {
                        foreach (ProtoPartModuleSnapshot m in p.modules)
                        {
                            if (m.moduleName == "CactEyeOccultationProcessor")
                            {
                                CelestialBody pBody = GetPlanetBody(FlightGlobals.Bodies[v.orbitSnapShot.ReferenceBodyIndex]);
                                if (!occExpAstCometDict.ContainsKey(pBody))
                                {
                                    int occTypeInt = m.moduleValues.SafeLoad("occTypeInt", 0);
                                    CactEyeOccultationProcessor.OccultationTypes occTypeEnum = (CactEyeOccultationProcessor.OccultationTypes)occTypeInt;
                                    string occType = m.moduleValues.SafeLoad("occType", "");
                                    double occTime = m.moduleValues.SafeLoad("occTime", 0f);

                                    Guid occAstCom;
                                    string occBody = "";
                                    string refBody = "";
                                    CelestialBody occBodyCB;
                                    CelestialBody refBodyCB;
                                    Vessel vTarget = null;

                                    if (occTypeEnum.ToString() != occType)
                                        Log.Error("occTypeEnum different than occtype");

                                    if (occTypeEnum != CactEyeOccultationProcessor.OccultationTypes.None && occTime > Planetarium.GetUniversalTime())
                                    {
                                        refBody = m.moduleValues.SafeLoad("refBody", "");
                                        refBodyCB = FlightGlobals.GetBodyByName(refBody);

                                        Log.Info("occultation: FOUND PROCESSOR: " + occType + " | " + occTime);

                                        switch (occTypeEnum)
                                        {
                                            case CactEyeOccultationProcessor.OccultationTypes.None:
                                                break;

                                            case CactEyeOccultationProcessor.OccultationTypes.Comet:
                                            case CactEyeOccultationProcessor.OccultationTypes.Asteroid:

                                                occAstCom = m.moduleValues.SafeLoad("occAstCom", Guid.Empty);
                                                Log.Info("occAstCom: : " + occAstCom);
                                                if (occAstCom != Guid.Empty)
                                                {
                                                    for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
                                                    {
                                                        if (FlightGlobals.Vessels[i].protoVessel.vesselID == occAstCom)
                                                        {
                                                            vTarget = FlightGlobals.Vessels[i];
                                                            break;
                                                        }
                                                    }
                                                    if (vTarget == null)
                                                        Log.Info("vTarget is null");
                                                    Log.Info("refBodyCB: " + refBodyCB.bodyName + ", vTarget: " + vTarget.name + ", occTime: " + occTime +
                                                        ", v.vesselID: " + v.vesselID);
                                                    AddOccultationExpEntry(refBodyCB, vTarget, occTime, v.vesselID);
                                                }
                                                break;

                                            case CactEyeOccultationProcessor.OccultationTypes.Moon:
                                            case CactEyeOccultationProcessor.OccultationTypes.Planet:
                                                occBody = m.moduleValues.SafeLoad("occBody", "");
                                                if (occBody != "")
                                                {
                                                    occBodyCB = FlightGlobals.GetBodyByName(occBody);
                                                    AddOccultationExpEntry(refBodyCB, occBodyCB, occTime, occType, v.vesselID);
                                                }
                                                break;

                                            default:
                                                Log.Error("Unknown occTypeEnum: " + occTypeEnum);
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
#endif

            foreach (CelestialBody body in bodyChildren.Keys)
            {
                if (body != Planetarium.fetch.Sun && !occExpAstCometDict.ContainsKey(body))
                    GenerateOccultationExp(body);
            }
            DumpOccultationExp();

            if (HighLogic.LoadedSceneIsFlight)
                StartCoroutine(SlowUpdate(60f));
        }

        private IEnumerator SlowUpdate(float delay)
        {
            while (true)
            {
                if (DoOnce)
                {
                    Log.Info("occultation:  sunDirection: " + Sun.Instance.sunDirection.ToString());
                    DoOnce = false;
                }
                if (FlightGlobals.ActiveVessel != null)
                    for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
                    {
                        CelestialBody body = FlightGlobals.Bodies[i];
                        if (body != Planetarium.fetch.Sun)
                        {
                            var bodyDist = body.GetAltitude(FlightGlobals.ship_position) + body.Radius; ;
                            var bodySize = Math.Acos(Math.Sqrt(Math.Pow(bodyDist, 2) - Math.Pow(body.Radius, 2)) / bodyDist) * (180 / Math.PI);
                            var bodyAngle = (body.position - FlightGlobals.ship_position).normalized;
                            if (bodyInfo.ContainsKey(body))
                            {
                                bodyInfo[body].bodyDist = bodyDist;
                                bodyInfo[body].bodySize = bodySize;
                                bodyInfo[body].bodyAngle = bodyAngle;
                            }
                            else
                            {
                                bodyInfo.Add(body, new BodyInfo(bodyDist, bodySize, bodyAngle));

                            }

#if false
                            if (bodyDist.ContainsKey(body))
                            {
                                bodyDist[body] = body.GetAltitude(FlightGlobals.ship_position) + body.Radius;
                            }
                            else
                            {
                                bodyDist.Add(body, body.GetAltitude(FlightGlobals.ship_position) + body.Radius);
                            }


                            if (bodySize.ContainsKey(body))
                            {
                                bodySize[body] = Math.Acos(Math.Sqrt(Math.Pow(bodyDist[body], 2) - Math.Pow(body.Radius, 2)) / bodyDist[body]) * (180 / Math.PI);
                            }
                            else
                            {
                                bodySize.Add(body, Math.Acos(Math.Sqrt(Math.Pow(bodyDist[body], 2) - Math.Pow(body.Radius, 2)) / bodyDist[body]) * (180 / Math.PI));
                            }

                            if (bodyAngle.ContainsKey(body))
                            {
                                bodyAngle[body] = (body.position - FlightGlobals.ship_position).normalized;
                            }
                            else
                            {
                                bodyAngle.Add(body, (body.position - FlightGlobals.ship_position).normalized);
                            }
#endif
                        }
                    }

                CelestialBody[] bodyCheck = new CelestialBody[occExpAstCometDict.Keys.Count];
                int counter = 0;

                foreach (KeyValuePair<CelestialBody, OccultationRecord> pair in occExpAstCometDict)
                {
                    bodyCheck[counter++] = pair.Key;
                }
                for (int i = 0; i < bodyCheck.Length; i++)
                {
                    CelestialBody body = bodyCheck[i];
                    if (Planetarium.GetUniversalTime() > occExpAstCometDict[body].time)
                    {
                        Log.Info("occultation: REGENERATING OCCULTATION EXP FOR " + body.bodyName);
                        GenerateOccultationExp(body);
                    }
                }
                DumpOccultationExp("SlowUpdate");
                yield return new WaitForSeconds(delay);
            }
        }
        void DumpOccultationExp(string from = "")
        {
#if DEBUG
            if (from != "")
                Log.Info("DumpOccultationExp called from: " + from);
            Log.Info("DumpOccultationExp, occExpAstCometDict: " + ((occExpAstCometDict != null) ? "Data" : "Null"));
            foreach (var r in occExpAstCometDict.Values)
            {
                string line = "OccultationRecord: ";
                if (r.referencebody == null)
                {
                    if (r.body == null)
                        line += ", r.body is null";
                    else
                        line += ", body: " + r.body.name;
                    line += ", time: " + r.time + ", type: " + r.type;
                }
                else
                {
                    if (r.vessel != null)
                        line += ", referencebody: " + r.referencebody.name +
                            ", time: " + r.time +
                            ", type: " + r.type +
                            ", vessel: " + r.vessel.name;
                    else
                        line += ", referencebody: " + r.referencebody.name +
                            ", time: " + r.time +
                            ", type: " + r.type;

                }
                Log.Info(line);
            }
#endif
        }
        //Is this body covered by any other body?
        public static string CheckOccult(CelestialBody body)
        {
            //Log.Info("occultation: CheckOccult, body: " + body.bodyName);
            if (!bodyInfo.ContainsKey(body))
            {
                if (body.bodyName != "Sun")
                    Log.Error("occultation: CheckOccult, body not in bodyInfo: " + body.bodyName);
            }
            else
            {
#if false
                if (!bodyAngle.ContainsKey(body))
                {
                    if (body.bodyName != "Sun")
                        Log.Error("occultation: CheckOccult, body not in bodyDist: " + body.bodyName);
                }
#endif

                for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
                {
                    CelestialBody bodyC = FlightGlobals.Bodies[i];
                    if (bodyC.bodyName != "Sun")
                    {
                        //foreach (CelestialBody bodyC in FlightGlobals.Bodies)
                        //{
                        //if (!bodyDist.ContainsKey(bodyC))
                        //{
                        //    //Log.Info("Could not find body " + bodyC.bodyName);
                        //}
                        if (body.name != bodyC.name &&
                            bodyInfo[bodyC].bodyDist < bodyInfo[body].bodyDist &&
                            bodyInfo[bodyC].bodySize > bodyInfo[body].bodySize &&
                            Vector3d.Angle(bodyInfo[body].bodyAngle, bodyInfo[bodyC].bodyAngle) < bodyInfo[bodyC].bodySize)
#if false
                            bodyDist[bodyC] < bodyDist[body] && 
                            bodySize[bodyC] > bodySize[body] && 
                            Vector3d.Angle(bodyAngle[body], bodyAngle[bodyC]) < bodySize[bodyC])
#endif
                        {
                            return bodyC.name;
                        }
                    }
                }
            }
            return "";
        }

        public static string CheckOccult(Vessel vessel)
        {
            for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
            {
                CelestialBody bodyC = FlightGlobals.Bodies[i];
                //foreach (CelestialBody bodyC in FlightGlobals.Bodies)
                //{
                if (!bodyInfo.ContainsKey(bodyC))
                {
                    Log.Info("occultation: Could not find body " + bodyC.bodyName);
                }
                else
                {
                    if (bodyInfo[bodyC].bodyDist < Vector3d.Distance(FlightGlobals.ship_position, vessel.GetWorldPos3D()) &&
                        Vector3d.Angle((vessel.GetWorldPos3D() - FlightGlobals.ship_position).normalized, bodyInfo[bodyC].bodyAngle) < bodyInfo[bodyC].bodySize)
#if false
                        if (bodyDist[bodyC] < Vector3d.Distance(FlightGlobals.ship_position, vessel.GetWorldPos3D()) &&
                            Vector3d.Angle((vessel.GetWorldPos3D() - FlightGlobals.ship_position).normalized, bodyAngle[bodyC]) < bodySize[bodyC])
#endif
                    {
                        return bodyC.name;
                    }
                }
            }
            return "";
        }

        public static string Time()
        {
            return Time(Planetarium.GetUniversalTime());
        }

        public static string Time(double t)
        {
            int y;
            int d;
            int h;
            int m;
            int s;
            const double secondsPerMinute = 60.0;
            const double secondsPerHour = 3600.0;
            double secondsPerDay = Planetarium.fetch.Home.solarDayLength;
            double secondsPerYear = Planetarium.fetch.Home.orbit.period;

            y = (int)Math.Floor(t / secondsPerYear);
            d = (int)Math.Floor((t - (y * secondsPerYear)) / secondsPerDay); //6 hours per Kerbin day
            h = (int)Math.Floor((t - (y * secondsPerYear) - (d * secondsPerDay)) / secondsPerHour);
            m = (int)Math.Floor((t - (y * secondsPerYear) - (d * secondsPerDay) - (h * secondsPerHour)) / secondsPerMinute);
            s = (int)Math.Floor(t - (y * secondsPerYear) - (d * secondsPerDay) - (h * secondsPerHour) - (m * secondsPerMinute));
            y += 1; //starts from year 1
            d += 1; //no day 0 either

            return y + "y-" + d + "d-" + h + "h-" + m + "m-" + s + "s";
        }

        public static void GenerateOccultationExp(CelestialBody planetBody)
        {
            Log.Info("occultation: GENERATING EXP FOR " + planetBody.bodyName);

            if (occExpAstCometDict.ContainsKey(planetBody))
                RemoveOccultationExpEntry(planetBody);


            double TimeToWait = InitialSetup.Random.Range(86400, 691200); //Anywhere between 4 Kerbin days (1 Earth day) and 32 Kerbin days (8 Earth days)
            float rndNum = InitialSetup.Random.Range(0f, 1f);

            Log.Info("GenrateOccultationExp, TimeToWait: " + TimeToWait + ", rndNum: " + rndNum);
            Log.Info("occultation: TIME: " + (TimeToWait + Planetarium.GetUniversalTime()) + " | " + Time(TimeToWait + Planetarium.GetUniversalTime()) + " | N = " + rndNum);

            // For each vessel, look for Asteroids which are being tracked and not yet in the astList
            // If found, add to astList, when done, add all entries in astList to OccultationExperiments
            // If none found, regen rndNum for a planet or moon instead
            rndNum = 1;
            if (rndNum > 0.9f)
            {
                Log.Info("rndNum > .9f, planetBody: " + planetBody.GetName() + ", isHomeWorld: " + planetBody.isHomeWorld + ", FlightGlobals.Vessels.Count: " + FlightGlobals.Vessels.Count);
                if (!planetBody.isHomeWorld)  // No asteroid exp. when around homeworld
                {
                    List<Vessel> astList = new List<Vessel>();
                    for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
                    {
                        Vessel v = FlightGlobals.Vessels[i];

                        for (int i1 = 0; i1 < v.protoVessel.protoPartSnapshots.Count; i1++)
                        {
                            if (v.protoVessel.protoPartSnapshots[i1].partName == "PotatoRoid" ||
                                v.protoVessel.protoPartSnapshots[i1].partName == "PotatoComet")
                            {
                                if (v.DiscoveryInfo.trackingStatus.Value == "Tracking" && !astList.Contains(v))
                                    astList.Add(v);
                            }
#if false
                            for (int i2 = 0; i2 < v.protoVessel.protoPartSnapshots[i1].modules.Count; i2++)
                            {
                                Log.Info("v.name: " + v.name + ", part: " + v.protoVessel.protoPartSnapshots[i1].partName +
                                    ", module: " + v.protoVessel.protoPartSnapshots[i1].modules[i2].moduleName);
                                ProtoPartModuleSnapshot m = v.protoVessel.protoPartSnapshots[i1].modules[i2];
                                if (m.moduleName == "ModuleAsteroid")
                                {
                                    if (v.DiscoveryInfo.trackingStatus.Value == "Tracking" && !astList.Contains(v))
                                        astList.Add(v);
                                }
                            }
#endif
                        }
                    }

                    if (astList.Count > 0)
                    {
                        int i = InitialSetup.Random.Range(0, astList.Count - 1);
                        AddOccultationExpEntry(planetBody, astList[i], Planetarium.GetUniversalTime() + TimeToWait, Guid.Empty);
                        return;
                    }
                    else
                        rndNum = InitialSetup.Random.Range(0f, 0.9f);
                }
                else
                    rndNum = InitialSetup.Random.Range(0f, 0.9f);
            }

            // For each planet, find all moons and add to OccultationExperiments
            if (rndNum < 0.4f)
            {
                if (bodyChildren.ContainsKey(planetBody))
                {
                    List<CelestialBody> bodies = new List<CelestialBody>(bodyChildren[planetBody]);
                    int i = InitialSetup.Random.Range(0, bodies.Count - 1);
                    AddOccultationExpEntry(planetBody, bodies[i], Planetarium.GetUniversalTime() + TimeToWait, "Moon", Guid.Empty);
                    return;
                }
                else
                    rndNum = InitialSetup.Random.Range(0.4f, 0.9f); //if reference planet has no moons, use another planet instead
            }

            if (rndNum < 0.6f)
            {
                List<CelestialBody> planets = new List<CelestialBody>(bodyChildren[planetBody]);
                planets.Remove(planetBody); // don't select current reference body!

                int i = InitialSetup.Random.Range(0, planets.Count - 1);
                if (bodyChildren.ContainsKey(planets[i]))
                {
                    List<CelestialBody> bodies = bodyChildren[planets[i]];
                    int k = InitialSetup.Random.Range(0, bodies.Count - 1);
                    AddOccultationExpEntry(planetBody, bodies[k], Planetarium.GetUniversalTime() + TimeToWait, "Moon", Guid.Empty);
                    return;
                }
                else
                {
                    AddOccultationExpEntry(planetBody, planets[i], Planetarium.GetUniversalTime() + TimeToWait, "Planet", Guid.Empty); //if the other planet has no moons, just select that planet
                    return;
                }
            }

            if (rndNum <= 0.9f)
            {
                List<CelestialBody> planets = new List<CelestialBody>(bodyChildren[Planetarium.fetch.Sun]);
                planets.Remove(planetBody); // don't select current reference body!

                int i = InitialSetup.Random.Range(0, planets.Count - 1);
                AddOccultationExpEntry(planetBody, planets[i], Planetarium.GetUniversalTime() + TimeToWait, "Planet", Guid.Empty);
            }
        }

        /// <summary>
        /// Add entry for target being a planet or mooon
        /// </summary>
        /// <param name="referenceBody"></param>
        /// <param name="targetBody"></param>
        /// <param name="time"></param>
        /// <param name="type"></param>
        public static void AddOccultationExpEntry(CelestialBody referenceBody, CelestialBody targetBody, double time, string type, Guid assignedTo)
        {
            if (referenceBody == null)
                Log.Info("1 referenceBody is null");
            if (targetBody == null)
                Log.Info("1 targetBody is null");
            Log.Info("1 occultation: AddOccultationExpEntry, referenceBody: " + referenceBody.bodyName +
                ", targetBody: " + targetBody.bodyName + ", time: " + time.ToString("F0") + ", type: " + type);

            occExpAstCometDict.Add(referenceBody, new OccultationRecord(referenceBody, time, type, targetBody));
            occExpAstCometDict[referenceBody].AssignTo = assignedTo;

            //saveNode.Save(KSPUtil.ApplicationRootPath + "GameData/CactEye/ExperimentSaveData.cfg");
        }

        /// <summary>
        /// Add entry for target being a vessel (either asteroid or Comet)
        /// </summary>
        /// <param name="referenceBody"></param>
        /// <param name="targetAsteroid"></param>
        /// <param name="time"></param>
        public static void AddOccultationExpEntry(CelestialBody referenceBody, Vessel targetAsteroid, double time, Guid assignedTo)
        {
            Log.Info("2 occultation: AddOccultationExpEntry, referenceBody: " + referenceBody.bodyName + ", targetAsteroid: " + targetAsteroid.GetName() + ", time: " + time.ToString("F0"));

            string type = (targetAsteroid.protoVessel.protoPartSnapshots[0].partName == "PotatoRoid" ? "Asteroid" : "Comet");

            occExpAstCometDict.Add(referenceBody, new OccultationRecord(referenceBody, time, type, targetAsteroid));
            occExpAstCometDict[referenceBody].AssignTo = assignedTo;

            //saveNode.Save(KSPUtil.ApplicationRootPath + "GameData/CactEye/ExperimentSaveData.cfg");
        }

        public static void RemoveOccultationExpEntry(CelestialBody referenceBody)
        {
            Log.Info("occultation: removing " + referenceBody.bodyName);
            if (occExpAstCometDict.ContainsKey(referenceBody))
                occExpAstCometDict.Remove(referenceBody);

#if false
            if (occultationExpTypes.ContainsKey(referenceBody))
            {
                if (occultationExpTypes[referenceBody] == "Asteroid" ||
                    occultationExpTypes[referenceBody] == "Comet")
                    occExpAstCometDict.Remove()
                else
                    occExpBodiesDict.Remove(referenceBody);
                    //occultationExpBodies.Remove(referenceBody);
                //occultationExpTimes.Remove(referenceBody);
                //occultationExpTypes.Remove(referenceBody);

                ////ConfigNode node = occultationExpNode.GetNodes("OccultationExperiment").Where(n => n.GetValue("referenceBodyName") == referenceBody.bodyName).First();
                //saveNode.RemoveNode(referenceBody.bodyName);
            }
#endif
        }

        public static string GetOccultationExpTimeString(CelestialBody referenceBody)
        {
            string r = Time(occExpAstCometDict[referenceBody].time - 30f); //we're subtracting thirty because the time recorded is the LIMIT, this is what we tell the user to shoot for
            r = r.Replace("-", " ");
            return r;
        }

        public static CelestialBody GetPlanetBody(CelestialBody body)
        {
            if (body == Planetarium.fetch.Sun)
                return null;

            CelestialBody referenceBody = body.referenceBody; //Reference body is supposed to be the main planet the telescope is in the SOI of
            if (referenceBody == Planetarium.fetch.Sun)
                referenceBody = body;

            return referenceBody;
        }
    }
}
