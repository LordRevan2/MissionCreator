﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MissionCreator.Editor.NestedMenus;
using MissionCreator.SerializableData.Cutscenes;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace MissionCreator.CutsceneEditor
{
    public class TimelineMarkerMenu : UIMenu, INestedMenu
    {
        public List<UIMenu> Children { get; set; }
        public CutsceneUi GrandParent;

        public bool CanMoveMarker
        {
            get
            {
                if (MenuItems.Count == 0)
                    return true;
                return !(MenuItems[CurrentSelection] is MenuListItem);
            }
        }

        public TimelineMarkerMenu(CutsceneUi grandpa) : base("Cutscene Creator", "MARKER")
        {
            Children = new List<UIMenu>();
            MouseControlsEnabled = false;
            ResetKey(Common.MenuControls.Up);
            ResetKey(Common.MenuControls.Down);
            SetKey(Common.MenuControls.Up, GameControl.CellphoneUp, 0);
            SetKey(Common.MenuControls.Down, GameControl.CellphoneDown, 0);

            GrandParent = grandpa;
        }

        public void BuildFor(TimeMarker marker)
        {
            Clear();
            Children.Clear();
            if (marker == null)
            {
                #region CreateMarker
                {
                    var item = new NativeMenuItem("Create new Camera Marker");
                    AddItem(item);
                    item.Activated += (sender, selectedItem) =>
                    {
                        var newM = new CameraMarker()
                        {
                            Time = GrandParent.CurrentTimestamp,
                            CameraPos = Editor.Editor.MainCamera.Position,
                            CameraRot = Editor.Editor.MainCamera.Rotation,
                        };
                        GrandParent.Markers.Add(newM);
                        BuildFor(newM);
                    };
                }
                {
                    var item = new NativeMenuItem("Create new Subtitle");
                    AddItem(item);
                    item.Activated += (sender, selectedItem) =>
                    {
                        var newM = new SubtitleMarker
                        {
                            Time = GrandParent.CurrentTimestamp,
                            Duration = 3000
                        };
                        GrandParent.Markers.Add(newM);
                        BuildFor(newM);
                    };
                }
                #endregion
                RefreshIndex();
                return;
            }
            var timeList =
                new List<dynamic>(Enumerable.Range(0, (int)(GrandParent.CurrentCutscene.Length/100f) + 1).Select(n => (dynamic) (n/10f)));

            {
                var item = new NativeMenuItem("Remove This Marker");
                item.Activated += (sender, selectedItem) =>
                {
                    GrandParent.Markers.Remove(marker);
                    BuildFor(null);
                };
                AddItem(item);
            }

            if (marker is CameraMarker)
            {
                var objList =
                    StaticData.StaticLists.InterpolationList.Select(x => (dynamic) (((InterpolationStyle) x).ToString()))
                        .ToList();
                var item = new MenuListItem("Interpolation",
                    objList,
                    StaticData.StaticLists.InterpolationList.IndexOf(((CameraMarker)marker).Interpolation));
                AddItem(item);
                item.OnListChanged += (sender, index) =>
                {
                    ((CameraMarker) marker).Interpolation =
                        (InterpolationStyle) StaticData.StaticLists.InterpolationList[index];
                };
            }
            else if (marker is SubtitleMarker)
            {
                {
                    var indx = (dynamic)((SubtitleMarker)marker).Duration / 1000f;
                    var item = new MenuListItem("Duration", timeList, timeList.IndexOf(indx == -1 ? 0 : indx));
                    AddItem(item);

                    item.OnListChanged += (sender, index) =>
                    {
                        var floatPointTime = float.Parse(((MenuListItem)sender).IndexToItem(index).ToString(), CultureInfo.InvariantCulture);
                        ((SubtitleMarker)marker).Duration = (int)(floatPointTime * 1000);
                    };
                }
                
                #region Text
                {
                    var item = new NativeMenuItem("Text");
                    if (string.IsNullOrEmpty(((SubtitleMarker)marker).Content))
                        item.SetRightBadge(NativeMenuItem.BadgeStyle.Alert);
                    else
                    {
                        var title = ((SubtitleMarker)marker).Content;
                        item.SetRightLabel(title.Length > 20 ? title.Substring(0, 20) + "..." : title);
                    }

                    item.Activated += (sender, selectedItem) =>
                    {
                        GameFiber.StartNew(delegate
                        {
                            ResetKey(Common.MenuControls.Back);
                            Editor.Editor.DisableControlEnabling = true;
                            string title = Util.GetUserInput();
                            if (string.IsNullOrEmpty(title))
                            {
                                item.SetRightBadge(NativeMenuItem.BadgeStyle.Alert);
                                ((SubtitleMarker)marker).Content = null;
                                SetKey(Common.MenuControls.Back, GameControl.CellphoneCancel, 0);
                                Editor.Editor.DisableControlEnabling = false;
                                return;
                            }
                            item.SetRightBadge(NativeMenuItem.BadgeStyle.None);
                            title = Regex.Replace(title, "-=", "~");
                            ((SubtitleMarker)marker).Content = title;
                            selectedItem.SetRightLabel(title.Length > 20 ? title.Substring(0, 20) + "..." : title);
                            SetKey(Common.MenuControls.Back, GameControl.CellphoneCancel, 0);
                            Editor.Editor.DisableControlEnabling = false;
                        });
                    };
                    AddItem(item);
                }
                #endregion

            }

            {
                var indx = (dynamic) marker.Time/1000f;
                var item = new MenuListItem("Time", timeList, timeList.IndexOf(indx == -1 ? 0 : indx));
                AddItem(item);

                item.OnListChanged += (sender, index) =>
                {
                    var floatPointTime = float.Parse(((MenuListItem) sender).IndexToItem(index).ToString(), CultureInfo.InvariantCulture);
                    marker.Time = (int)(floatPointTime*1000);
                    GrandParent.CurrentTimestamp = marker.Time;
                };
            }

            RefreshIndex();
        }

        public void Process()
        {

        }
    }
}