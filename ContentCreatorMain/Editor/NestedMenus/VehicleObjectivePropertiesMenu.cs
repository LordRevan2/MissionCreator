﻿using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ContentCreator.SerializableData.Objectives;
using Rage;
using Rage.Native;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace ContentCreator.Editor.NestedMenus
{
    public class VehicleObjectivePropertiesMenu : UIMenu, INestedMenu
    {
        public VehicleObjectivePropertiesMenu() : base("", "VEHICLE OBJECTIVE PROPERTIES", new Point(0, -107))
        {
            Children = new List<UIMenu>();
            SetBannerType(new ResRectangle());
            MouseControlsEnabled = false;
            ResetKey(Common.MenuControls.Up);
            ResetKey(Common.MenuControls.Down);
            SetKey(Common.MenuControls.Up, GameControl.CellphoneUp, 0);
            SetKey(Common.MenuControls.Down, GameControl.CellphoneDown, 0);
        }

        public List<UIMenu> Children { get; set; }

        public void BuildFor(SerializableData.Objectives.SerializableVehicleObjective actor)
        {
            Clear();

            #region SpawnAfter
            {
                var item = new MenuListItem("Spawn After Objective", StaticData.StaticLists.NumberMenu, actor.SpawnAfter);

                item.OnListChanged += (sender, index) =>
                {
                    actor.SpawnAfter = index;
                };

                AddItem(item);
            }
            #endregion

            #region ObjectiveIndex
            {
                var item = new MenuListItem("Objective Index", StaticData.StaticLists.ObjectiveIndexList, actor.ActivateAfter-1);

                item.OnListChanged += (sender, index) =>
                {
                    actor.ActivateAfter = index + 1;


                    if (string.IsNullOrEmpty(Editor.CurrentMission.ObjectiveNames[actor.ActivateAfter]))
                    {
                        MenuItems[2].SetRightBadge(NativeMenuItem.BadgeStyle.Alert);
                        MenuItems[2].SetRightLabel("");
                    }
                    else
                    {
                        var title = Editor.CurrentMission.ObjectiveNames[actor.ActivateAfter];
                        MenuItems[2].SetRightLabel(title.Length > 20 ? title.Substring(0, 20) + "..." : title);
                        MenuItems[2].SetRightBadge(NativeMenuItem.BadgeStyle.None);
                    }
                };

                AddItem(item);
            }
            #endregion 
            // TODO: Change NumberMenu to max num of objectives in mission

            // Note: if adding items before weapons, change item order in VehiclePropertiesMenu
            
            #region Objective Name
            {
                var item = new NativeMenuItem("Objective Name");
                if (string.IsNullOrEmpty(Editor.CurrentMission.ObjectiveNames[actor.ActivateAfter]))
                    item.SetRightBadge(NativeMenuItem.BadgeStyle.Alert);
                else
                {
                    var title = Editor.CurrentMission.ObjectiveNames[actor.ActivateAfter];
                    item.SetRightLabel(title.Length > 20 ? title.Substring(0, 20) + "..." : title);
                }

                item.Activated += (sender, selectedItem) =>
                {
                    GameFiber.StartNew(delegate
                    {
                        ResetKey(Common.MenuControls.Back);
                        Editor.DisableControlEnabling = true;
                        string title = Util.GetUserInput();
                        if (string.IsNullOrEmpty(title))
                        {
                            item.SetRightBadge(NativeMenuItem.BadgeStyle.Alert);
                            Editor.CurrentMission.ObjectiveNames[actor.ActivateAfter] = "";
                            SetKey(Common.MenuControls.Back, GameControl.CellphoneCancel, 0);
                            Editor.DisableControlEnabling = false;
                            return;
                        }
                        item.SetRightBadge(NativeMenuItem.BadgeStyle.None);
                        title = Regex.Replace(title, "-=", "~");
                        Editor.CurrentMission.ObjectiveNames[actor.ActivateAfter] = title;
                        selectedItem.SetRightLabel(title.Length > 20 ? title.Substring(0, 20) + "..." : title);
                        SetKey(Common.MenuControls.Back, GameControl.CellphoneCancel, 0);
                    });
                };
                AddItem(item);
            }
            #endregion

            #region Health
            {
                var listIndex = actor.Health == 0
                    ? StaticData.StaticLists.VehicleHealthChoses.FindIndex(n => n == (dynamic)1000)
                    : StaticData.StaticLists.VehicleHealthChoses.FindIndex(n => n == (dynamic)actor.Health);

                var item = new MenuListItem("Health", StaticData.StaticLists.VehicleHealthChoses, listIndex);

                item.OnListChanged += (sender, index) =>
                {
                    int newAmmo = int.Parse(((MenuListItem)sender).IndexToItem(index).ToString(), CultureInfo.InvariantCulture);
                    actor.Health = newAmmo;
                };

                AddItem(item);
            }
            #endregion
            
            #region Passengers
            {
                var item = new NativeMenuItem("Occupants");
                AddItem(item);
                if (((Vehicle)actor.GetVehicle()).HasOccupants)
                {
                    var newMenu = new UIMenu("", "OCCUPANTS", new Point(0, -107));
                    newMenu.MouseControlsEnabled = false;
                    newMenu.SetBannerType(new ResRectangle());
                    var occupants = ((Vehicle)actor.GetVehicle()).Occupants;
                    for (int i = 0; i < occupants.Length; i++)
                    {
                        var ped = occupants[i];
                        var type = Editor.GetEntityType(ped);
                        if (type == Editor.EntityType.NormalActor)
                        {
                            var act = Editor.CurrentMission.Actors.FirstOrDefault(a => a.GetEntity().Handle.Value == ped.Handle.Value);
                            if (act == null) continue;
                            var routedItem = new NativeMenuItem(i == 0 ? "Driver" : "Passenger #" + i);
                            routedItem.Activated += (sender, selectedItem) =>
                            {
                                Editor.DisableControlEnabling = true;
                                Editor.EnableBasicMenuControls = true;
                                var propMenu = new ActorPropertiesMenu();
                                propMenu.BuildFor(act);
                                propMenu.MenuItems[2].Enabled = false;
                                propMenu.OnMenuClose += _ =>
                                {
                                    newMenu.Visible = true;
                                };

                                newMenu.Visible = false;
                                propMenu.Visible = true;
                                GameFiber.StartNew(delegate
                                {
                                    while (propMenu.Visible)
                                    {
                                        propMenu.ProcessControl();
                                        propMenu.Draw();
                                        propMenu.Process();
                                        GameFiber.Yield();
                                    }
                                });

                            };
                            newMenu.AddItem(routedItem);
                        }
                        else if (type == Editor.EntityType.ObjectiveActor)
                        {
                            var act = Editor.CurrentMission.Objectives
                                .OfType<SerializableActorObjective>()
                                .FirstOrDefault(a => a.GetPed().Handle.Value == ped.Handle.Value);
                            if (act == null) continue;
                            var routedItem = new NativeMenuItem(i == 0 ? "Objective Driver" : "Objective Passenger #" + i);
                            routedItem.Activated += (sender, selectedItem) =>
                            {
                                Editor.DisableControlEnabling = true;
                                Editor.EnableBasicMenuControls = true;
                                var propMenu = new ActorObjectivePropertiesMenu();
                                propMenu.BuildFor(act);
                                propMenu.MenuItems[2].Enabled = false;
                                propMenu.OnMenuClose += _ =>
                                {
                                    newMenu.Visible = true;
                                };

                                newMenu.Visible = false;
                                propMenu.Visible = true;
                                GameFiber.StartNew(delegate
                                {
                                    while (propMenu.Visible)
                                    {
                                        propMenu.ProcessControl();
                                        propMenu.Draw();
                                        propMenu.Process();
                                        GameFiber.Yield();
                                    }
                                });

                            };
                            newMenu.AddItem(routedItem);
                        }
                        
                    }
                    BindMenuToItem(newMenu, item);
                    newMenu.RefreshIndex();
                    Children.Add(newMenu);
                }
                else
                {
                    item.Enabled = false;
                }

            }
            #endregion

            RefreshIndex();
        }

        public void Process()
        {
            Children.ForEach(x =>
            {
                x.ProcessControl();
                x.Draw();
            });
        }
    }
}