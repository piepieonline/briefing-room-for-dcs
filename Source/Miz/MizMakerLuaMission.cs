﻿/*
==========================================================================
This file is part of Briefing Room for DCS World, a mission
generator for DCS World, by @akaAgar
(https://github.com/akaAgar/briefing-room-for-dcs)

Briefing Room for DCS World is free software: you can redistribute it
and/or modify it under the terms of the GNU General Public License
as published by the Free Software Foundation, either version 3 of
the License, or (at your option) any later version.

Briefing Room for DCS World is distributed in the hope that it will
be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Briefing Room for DCS World.
If not, see https://www.gnu.org/licenses/
==========================================================================
*/

using BriefingRoom4DCSWorld.DB;
using BriefingRoom4DCSWorld.Mission;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BriefingRoom4DCSWorld.Miz
{
    /// <summary>
    /// Creates the "Mission" entry in the MIZ file.
    /// </summary>
    public class MizMakerLuaMission : IDisposable
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public MizMakerLuaMission() { }

        /// <summary>
        /// <see cref="IDisposable"/> implementation.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Generates the content of the Lua file.
        /// </summary>
        /// <param name="mission">An HQ4DCS mission.</param>
        /// <returns>The contents of the Lua file.</returns>
        public string MakeLua(DCSMission mission)
        {
            int i;

            string lua = LuaTools.ReadIncludeLuaFile("Mission.lua");

            LuaTools.ReplaceKey(ref lua, "TheaterID", Database.Instance.GetEntry<DBEntryTheater>(mission.Theater).DCSID);
            LuaTools.ReplaceKey(ref lua, "PlayerCoalition", mission.CoalitionPlayer.ToString().ToLowerInvariant());

            LuaTools.ReplaceKey(ref lua, "DateDay", mission.DateTime.Day);
            LuaTools.ReplaceKey(ref lua, "DateMonth", (int)mission.DateTime.Month + 1);
            LuaTools.ReplaceKey(ref lua, "DateYear", mission.DateTime.Year);
            LuaTools.ReplaceKey(ref lua, "StartTime", mission.DateTimeTotalSeconds);

            LuaTools.ReplaceKey(ref lua, "WeatherCloudsBase", mission.Weather.CloudBase);
            LuaTools.ReplaceKey(ref lua, "WeatherCloudsDensity", mission.Weather.CloudsDensity);
            LuaTools.ReplaceKey(ref lua, "WeatherCloudsPrecipitation", (int)mission.Weather.CloudsPrecipitation);
            LuaTools.ReplaceKey(ref lua, "WeatherCloudsThickness", mission.Weather.CloudsThickness);
            LuaTools.ReplaceKey(ref lua, "WeatherDustDensity", mission.Weather.DustDensity);
            LuaTools.ReplaceKey(ref lua, "WeatherDustEnabled", mission.Weather.DustEnabled);
            LuaTools.ReplaceKey(ref lua, "WeatherFogEnabled", mission.Weather.Turbulence);
            LuaTools.ReplaceKey(ref lua, "WeatherFogThickness", mission.Weather.FogThickness);
            LuaTools.ReplaceKey(ref lua, "WeatherFogVisibility", mission.Weather.FogVisibility);
            LuaTools.ReplaceKey(ref lua, "WeatherGroundTurbulence", mission.Weather.Turbulence);
            LuaTools.ReplaceKey(ref lua, "WeatherQNH", mission.Weather.QNH);
            LuaTools.ReplaceKey(ref lua, "WeatherTemperature", mission.Weather.Temperature);
            LuaTools.ReplaceKey(ref lua, "WeatherVisibilityDistance", mission.Weather.Visibility);
            for (i = 0; i < 3; i++)
            {
                LuaTools.ReplaceKey(ref lua, $"WeatherWind{i + 1}", mission.Weather.WindSpeed[i]);
                LuaTools.ReplaceKey(ref lua, $"WeatherWind{i + 1}Dir", mission.Weather.WindDirection[i]);
            }
            var neutrals = Enum.GetValues(typeof(Country)).Cast<Country>().Where(x => !mission.CountryBlues.Contains(x) && !mission.CountryReds.Contains(x));
            LuaTools.ReplaceKey(ref lua, "Neutrals", Toolbox.ListToLuaString(neutrals.Cast<int>()));
            LuaTools.ReplaceKey(ref lua, "Reds", Toolbox.ListToLuaString(mission.CountryReds.Cast<int>()));
            LuaTools.ReplaceKey(ref lua, "Blues", Toolbox.ListToLuaString(mission.CountryBlues.Cast<int>()));

            LuaTools.ReplaceKey(ref lua, "BullseyeBlueX", mission.Bullseye[(int)Coalition.Blue].X);
            LuaTools.ReplaceKey(ref lua, "BullseyeBlueY", mission.Bullseye[(int)Coalition.Blue].Y);
            LuaTools.ReplaceKey(ref lua, "BullseyeRedX", mission.Bullseye[(int)Coalition.Red].X);
            LuaTools.ReplaceKey(ref lua, "BullseyeRedY", mission.Bullseye[(int)Coalition.Red].Y);


            LuaTools.ReplaceKey(ref lua, "MissionName", mission.MissionName);
            LuaTools.ReplaceKey(ref lua, "BriefingDescription", mission.BriefingTXT, true);

            CreateUnitGroups(ref lua, mission); // Create unit groups
    
            Debug.DebugLog.Instance.WriteLine("Building Airbase");
            DBEntryTheaterAirbase airbase =
                (from DBEntryTheaterAirbase ab in Database.Instance.GetEntry<DBEntryTheater>(mission.Theater).Airbases
                where ab.DCSID == mission.InitialAirbaseID select ab).FirstOrDefault();
            LuaTools.ReplaceKey(ref lua, "MissionAirbaseID", mission.InitialAirbaseID);
            LuaTools.ReplaceKey(ref lua, "MissionAirbaseX", airbase.Coordinates.X);
            LuaTools.ReplaceKey(ref lua, "MissionAirbaseY", airbase.Coordinates.Y);

            // The following replacements must be performed after unit groups and player waypoints have been added
            LuaTools.ReplaceKey(ref lua, "PlayerGroupID", mission.PlayerGroupID);
            LuaTools.ReplaceKey(ref lua, "InitialWPName", Database.Instance.Common.WPNameInitial);
            LuaTools.ReplaceKey(ref lua, "FinalWPName", Database.Instance.Common.WPNameFinal);
            LuaTools.ReplaceKey(ref lua, "FinalWPName", Database.Instance.Common.WPNameFinal); //Duplicate

            switch (mission.PlayerStartLocation)
            {
                default: // case PlayerStartLocation.ParkingCold
                    LuaTools.ReplaceKey(ref lua, "PlayerStartingAction", "From Parking Area");
                    LuaTools.ReplaceKey(ref lua, "PlayerStartingType", "TakeOffParking");

                    LuaTools.ReplaceKey(ref lua, "PlayerEscortStartingAction", "From Parking Area"); // Player starts cold, AI escorts start cold too
                    LuaTools.ReplaceKey(ref lua, "PlayerEscortStartingType", "TakeOffParking");
                    LuaTools.ReplaceKey(ref lua, "PlayerEscortStartingAltitude", 13);
                    break;
                case PlayerStartLocation.ParkingHot:
                    LuaTools.ReplaceKey(ref lua, "PlayerStartingAction", "From Parking Area Hot");
                    LuaTools.ReplaceKey(ref lua, "PlayerStartingType", "TakeOffParkingHot");

                    LuaTools.ReplaceKey(ref lua, "PlayerEscortStartingAction", "From Parking Area Hot"); // Player starts hot, AI escorts start hot too
                    LuaTools.ReplaceKey(ref lua, "PlayerEscortStartingType", "TakeOffParkingHot");
                    LuaTools.ReplaceKey(ref lua, "PlayerEscortStartingAltitude", 13);
                    break;
                case PlayerStartLocation.Runway:
                    LuaTools.ReplaceKey(ref lua, "PlayerStartingAction", "From Runway");
                    LuaTools.ReplaceKey(ref lua, "PlayerStartingType", "TakeOff");

                    LuaTools.ReplaceKey(ref lua, "PlayerEscortStartingAction", "Turning Point"); // Player starts on runway, AI escorts start in air above him
                    LuaTools.ReplaceKey(ref lua, "PlayerEscortStartingType", "Turning Point");
                    LuaTools.ReplaceKey(ref lua, "PlayerEscortStartingAltitude", 10000 * Toolbox.FEET_TO_METERS);
                    break;
            }

            LuaTools.ReplaceKey(ref lua, "MissionID", mission.UniqueID);
            LuaTools.ReplaceKey(ref lua, "ForcedOptions", GetForcedOptionsLua(mission));

            return lua;
        }

        /// <summary>
        /// Returns a string of Lua containing the forced mission options.
        /// </summary>
        /// <param name="mission">The DCS mission for which forced options should be generated</param>
        /// <returns>Values for a Lua table, as a string</returns>
        private string GetForcedOptionsLua(DCSMission mission)
        {
            string forcedOptionsLua = "";

            foreach (RealismOption realismOption in mission.RealismOptions)
            {
                switch (realismOption)
                {
                    case RealismOption.BirdStrikes: forcedOptionsLua += "[\"birds\"] = 300,"; break;
                    case RealismOption.HideLabels: forcedOptionsLua += "[\"labels\"] = 0,"; break;
                    case RealismOption.NoBDA: forcedOptionsLua += "[\"RBDAI\"] = false,\r\n"; break;
                    case RealismOption.NoCheats: forcedOptionsLua += "[\"immortal\"] = false, [\"fuel\"] = false, [\"weapons\"] = false,"; break;
                    case RealismOption.NoCrashRecovery: forcedOptionsLua += "[\"permitCrash\"] = false,"; break;
                    case RealismOption.NoEasyComms: forcedOptionsLua += "[\"easyCommunication\"] = false,"; break;
                    case RealismOption.NoExternalViews: forcedOptionsLua += "[\"externalViews\"] = false,"; break;
                    case RealismOption.NoGameMode: forcedOptionsLua += "[\"easyFlight\"] = false, [\"easyRadar\"] = false,"; break;
                    case RealismOption.NoOverlays: forcedOptionsLua += "[\"miniHUD\"] = false, [\"cockpitStatusBarAllowed\"] = false,"; break;
                    case RealismOption.NoPadlock: forcedOptionsLua += "[\"padlock\"] = false,"; break;
                    case RealismOption.RandomFailures: forcedOptionsLua += "[\"accidental_failures\"] = true,"; break;
                    case RealismOption.RealisticGEffects: forcedOptionsLua += "[\"geffect\"] = \"realistic\","; break;
                    case RealismOption.WakeTurbulence: forcedOptionsLua += "[\"wakeTurbulence\"] = true,"; break;
                }
            }

            // Some realism options are forced OFF when not explicitely enabled
            if (!mission.RealismOptions.Contains(RealismOption.BirdStrikes))
                forcedOptionsLua += "[\"birds\"] = 0,";
            else if (!mission.RealismOptions.Contains(RealismOption.RandomFailures))
                forcedOptionsLua += "[\"accidental_failures\"] = false,";
            else if (!mission.RealismOptions.Contains(RealismOption.NoBDA))
                forcedOptionsLua += "[\"RBDAI\"] = true,";

            forcedOptionsLua += $"[\"radio\"] = {(mission.RadioAssists ? "true" : "false")},";

            if (mission.CivilianTraffic == CivilianTraffic.Off)
                forcedOptionsLua += "[\"civTraffic\"] = \"\",";
            else
                forcedOptionsLua += $"[\"civTraffic\"] = \"{mission.CivilianTraffic.ToString().ToLowerInvariant()}\",";

            if (mission.RealismOptions.Contains(RealismOption.MapDisableAll))
                forcedOptionsLua += "[\"optionsView\"] = \"optview_onlymap\",";
            else if(mission.RealismOptions.Contains(RealismOption.MapDisableAllButSelf))
                forcedOptionsLua += "[\"optionsView\"] = \"optview_myaircraft\",";
            else if (mission.RealismOptions.Contains(RealismOption.MapDisableAllButUnitsKnown))
                forcedOptionsLua += "[\"optionsView\"] = \"optview_allies\",";

            return forcedOptionsLua;
        }

        private void CreatePlayerWaypoints(ref string groupLua, DCSMission mission, DBEntryUnit unitBP)
        {
            string waypointLua = LuaTools.ReadIncludeLuaFile("Mission\\WaypointPlayer.lua");
            string allWaypointsLua = "";

            for (int i = 0; i < mission.Waypoints.Count; i++)
            {
                string waypoint = waypointLua;
                LuaTools.ReplaceKey(ref waypoint, "Index", i + 2);
                LuaTools.ReplaceKey(ref waypoint, "Name", mission.Waypoints[i].Name);
                LuaTools.ReplaceKey(ref waypoint, "X", mission.Waypoints[i].Coordinates.X);
                LuaTools.ReplaceKey(ref waypoint, "Y", mission.Waypoints[i].Coordinates.Y);

                double waypointAltitude, waypointSpeed;
                if (unitBP == null) // Unit not found in the database, use default values for the unit category
                {
                    waypointAltitude = ((unitBP.Category == UnitCategory.Helicopter) ? 1500 : 20000) * Toolbox.FEET_TO_METERS;
                    waypointSpeed = ((unitBP.Category == UnitCategory.Helicopter) ? 90 : 300) * Toolbox.KNOTS_TO_METERS_PER_SECOND;
                }
                else
                {
                    waypointAltitude = unitBP.AircraftData.CruiseAltitude;
                    waypointSpeed = unitBP.AircraftData.CruiseAltitude;
                }

                waypointAltitude *= mission.Waypoints[i].AltitudeMultiplier;
                waypointSpeed *= mission.Waypoints[i].SpeedMultiplier;

                LuaTools.ReplaceKey(ref waypoint, "Altitude", waypointAltitude);
                LuaTools.ReplaceKey(ref waypoint, "Speed", waypointSpeed);

                allWaypointsLua += waypoint + "\n";
            }

            LuaTools.ReplaceKey(ref groupLua, "PlayerWaypoints", allWaypointsLua);
            LuaTools.ReplaceKey(ref groupLua, "LastPlayerWaypointIndex", mission.Waypoints.Count + 2);
        }

        private void CreateUnitGroups(ref string lua, DCSMission mission)
        {
            int i, j;

            for (i = 0; i < 2; i++){
                string coalitionLua = "";
                var k = 1;
                foreach (var country in (Coalition)i == Coalition.Blue? mission.CountryBlues: mission.CountryReds)
                {
                    string countryLua = LuaTools.ReadIncludeLuaFile($"Country");
                    LuaTools.ReplaceKey(ref countryLua, "Index", k);
                    LuaTools.ReplaceKey(ref countryLua, "DCSID", (int)country);
                    LuaTools.ReplaceKey(ref countryLua, "Name", Enum.GetName(typeof(Country), country));
                    for (j = 0; j < Toolbox.EnumCount<UnitCategory>(); j++)
                        CreateUnitGroups(ref countryLua, mission, (Coalition)i, (UnitCategory)j, country);
                    coalitionLua += countryLua;
                    k++;
                }
                LuaTools.ReplaceKey(ref lua, $"COUNTRYS{(Coalition)i}", coalitionLua);
            }
        }

        private void CreateUnitGroups(ref string lua, DCSMission mission, Coalition coalition, UnitCategory unitCategory, Country country)
        {
            int i, j;
            int groupIndex = 1;
            string allGroupsLua = "";

            foreach (DCSMissionUnitGroup group in mission.UnitGroups)
            {
                if ((group.Coalition != coalition) || // Group does not belong to this coalition
                    (group.Country != country) || // Group does not match country
                    (group.Category != unitCategory) || // Group does not belong to this unit category
                    (group.Units.Length == 0)) // Group doesn't contain any units
                    continue; // Ignore it

                string groupLua = LuaTools.ReadIncludeLuaFile($"Units\\{group.LuaGroup}");
                LuaTools.ReplaceKey(ref groupLua, "Index", groupIndex);
                LuaTools.ReplaceKey(ref groupLua, "X", group.Coordinates.X);
                LuaTools.ReplaceKey(ref groupLua, "Y", group.Coordinates.Y);
                LuaTools.ReplaceKey(ref groupLua, "X2", group.Coordinates2.X);
                LuaTools.ReplaceKey(ref groupLua, "Y2", group.Coordinates2.Y);
                LuaTools.ReplaceKey(ref groupLua, "Hidden", group.Flags.HasFlag(DCSMissionUnitGroupFlags.Hidden));
                LuaTools.ReplaceKey(ref groupLua, "ID", group.GroupID);
                LuaTools.ReplaceKey(ref groupLua, "FirstUnitID", group.Units[0].ID);
                LuaTools.ReplaceKey(ref groupLua, "Name", group.Name);
                LuaTools.ReplaceKey(ref groupLua, "ObjectiveAirbaseID", group.AirbaseID);

                if(group.CarrierId > 0)
                    LuaTools.ReplaceKey(ref groupLua, "LinkUnit", group.CarrierId);
                
                if (group.TACAN != null){
                    LuaTools.ReplaceKey(ref groupLua, "TacanFrequency", group.TACAN.freqency);
                    LuaTools.ReplaceKey(ref groupLua, "TacanCallSign", group.TACAN.callsign);
                    LuaTools.ReplaceKey(ref groupLua, "TacanChannel", group.TACAN.channel);
                    LuaTools.ReplaceKey(ref groupLua, "UnitID", group.Units[0].ID);
                }
                if(group.ILS > 0)
                    LuaTools.ReplaceKey(ref groupLua, "ILS", group.ILS);


                DBEntryUnit unitBP = Database.Instance.GetEntry<DBEntryUnit>(group.UnitID);
                if (unitBP == null) continue; // TODO: error message?

                // Group is a client group, requires player waypoints
                if (group.IsAPlayerGroup())
                    CreatePlayerWaypoints(ref groupLua, mission, unitBP);

                string allUnitsLua = "";
                for (i = 0; i < group.Units.Length; i++)
                {
                    string unitLua = LuaTools.ReadIncludeLuaFile($"Units\\{group.LuaUnit}");
                    Coordinates unitCoordinates = new Coordinates(group.Coordinates);
                    double unitHeading = 0;

                    if ((group.Category == UnitCategory.Static) || (group.Category == UnitCategory.Vehicle))
                    {
                        double randomSpreadAngle = Toolbox.RandomDouble(Math.PI * 2);
                        unitCoordinates += new Coordinates(Math.Cos(randomSpreadAngle) * 35, Math.Sin(randomSpreadAngle) * 35);
                        unitHeading = Toolbox.RandomDouble(Math.PI * 2);
                    }
                    else
                        unitCoordinates += new Coordinates(50 * i, 50 * i);

                    if ((group.Category == UnitCategory.Helicopter) || (group.Category == UnitCategory.Plane)) // Unit is an aircraft
                    {
                        if (unitBP == null)
                        {
                            LuaTools.ReplaceKey(ref groupLua, "Altitude", ((group.Category == UnitCategory.Helicopter) ? 1500 : 4572) * Toolbox.RandomDouble(.8, 1.2));
                            LuaTools.ReplaceKey(ref unitLua, "Altitude", ((group.Category == UnitCategory.Helicopter) ? 1500 : 4572) * Toolbox.RandomDouble(.8, 1.2));
                            LuaTools.ReplaceKey(ref groupLua, "EPLRS", false);
                            LuaTools.ReplaceKey(ref unitLua, "PayloadCommon", "");
                            LuaTools.ReplaceKey(ref unitLua, "PayloadPylons", "");
                            LuaTools.ReplaceKey(ref groupLua, "RadioBand", 0);
                            LuaTools.ReplaceKey(ref groupLua, "RadioFrequency", 124.0f);
                            LuaTools.ReplaceKey(ref groupLua, "Speed", ((group.Category == UnitCategory.Helicopter) ? 51 : 125) * Toolbox.RandomDouble(.9, 1.1));
                        }
                        else
                        {
                            LuaTools.ReplaceKey(ref groupLua, "Altitude", unitBP.AircraftData.CruiseAltitude);
                            LuaTools.ReplaceKey(ref unitLua, "Altitude", unitBP.AircraftData.CruiseAltitude);
                            LuaTools.ReplaceKey(ref groupLua, "EPLRS", unitBP.Flags.HasFlag(DBEntryUnitFlags.EPLRS));
                            LuaTools.ReplaceKey(ref unitLua, "PayloadCommon", unitBP.AircraftData.PayloadCommon);
                            LuaTools.ReplaceKey(ref groupLua, "RadioBand", (int)unitBP.AircraftData.RadioModulation);
                            LuaTools.ReplaceKey(ref groupLua, "RadioFrequency", unitBP.AircraftData.RadioFrequency);
                            LuaTools.ReplaceKey(ref groupLua, "Speed", unitBP.AircraftData.CruiseSpeed);

                            string pylonLua = "";
                            string[] payload = unitBP.AircraftData.GetPayload(group.Payload, mission.DateTime.Decade);
                            for (j = 0; j < DBEntryUnitAircraftData.MAX_PYLONS; j++)
                            {
                                string pylonCode = payload[j];
                                if (!string.IsNullOrEmpty(pylonCode))
                                    pylonLua += $"[{j + 1}] = {{[\"CLSID\"] = \"{pylonCode}\" }},\r\n";
                            }

                            LuaTools.ReplaceKey(ref unitLua, "PayloadPylons", pylonLua);
                        }
                    } else if((group.Category == UnitCategory.Ship)){
                        if(group.RadioFrequency > 0){
                        LuaTools.ReplaceKey(ref unitLua, "RadioBand", (int)group.RadioModulation);
                        LuaTools.ReplaceKey(ref unitLua, "RadioFrequency", group.RadioFrequency * 1000000);
                        } else {
                            LuaTools.ReplaceKey(ref unitLua, "RadioBand", 0);
                            LuaTools.ReplaceKey(ref unitLua, "RadioFrequency", 127.0f * 1000000);
                        }
                        LuaTools.ReplaceKey(ref groupLua, "Speed", group.Speed);
                    }

                    if (unitBP == null)
                        LuaTools.ReplaceKey(ref unitLua, "ExtraLua", "");
                    else
                        LuaTools.ReplaceKey(ref unitLua, "ExtraLua", unitBP.ExtraLua);

                    LuaTools.ReplaceKey(ref unitLua, "Callsign", group.CallsignLua); // Must be before "index" replacement, as is it is integrated in the callsign
                    LuaTools.ReplaceKey(ref unitLua, "Index", i + 1);
                    LuaTools.ReplaceKey(ref unitLua, "ID", group.Units[i].ID);
                    LuaTools.ReplaceKey(ref unitLua, "Type", group.Units[i].Type);
                    LuaTools.ReplaceKey(ref unitLua, "Name", $"{group.Name} {i + 1}");
                    if (group.Flags.HasFlag(DCSMissionUnitGroupFlags.FirstUnitIsPlayer))
                        LuaTools.ReplaceKey(ref unitLua, "Skill", (i == 0) ? DCSSkillLevel.Player : group.Skill, false);
                    else
                        LuaTools.ReplaceKey(ref unitLua, "Skill", group.Skill, false);
                    LuaTools.ReplaceKey(ref unitLua, "X", group.Units[i].Coordinates.X);
                    LuaTools.ReplaceKey(ref unitLua, "Y", group.Units[i].Coordinates.Y);
                    LuaTools.ReplaceKey(ref unitLua, "Heading", group.Units[i].Heading);
                    LuaTools.ReplaceKey(ref unitLua, "OnboardNumber", Toolbox.RandomInt(1, 1000).ToString("000"));
                    LuaTools.ReplaceKey(ref unitLua, "ParkingID", group.Units[i].ParkingSpot);

                    allUnitsLua += unitLua + "\n";
                }
                LuaTools.ReplaceKey(ref groupLua, "units", allUnitsLua);

                allGroupsLua += groupLua + "\n";
                
                groupIndex++;
            }

            LuaTools.ReplaceKey(ref lua, $"Groups{unitCategory}", allGroupsLua);
        }
    }
}
