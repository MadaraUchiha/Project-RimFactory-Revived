using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Reflection.Emit;

namespace ProjectRimFactory.Misc {
    /*********************************************************************
     * Glower_ColorPick:
     *   A comp that lets you change colors after a light source has
     *   been built.
     * To add this to your own mod:
     * 1.  Use your own namespace (be polite).
     * 2.  Change "PRF_ChangeColorGizmo" to point to your own translation
     *     key.  It should look something like:
     *     <YourMod_ChangeColorGizmo>({0})\nChange Color?</YourMod_ChangeColorGizmo>
     *     Add translation keys for your colors!
     *     <YourMod_White>white</YourMod_White>
     *     (safer to do "YourMod_..." - you never know if some other modder
     *      has already made "red" "Blood-colored" or "Red Wagon" or whatever)
     * 3.  Compile this into your C# project!  (Assembly not included)
     * 4.  Add the comp to your light source instead of CompGlower!
     * <comps>
     *  <li Class="YourNamespace.CompProperties_Glower_ColorPick">
     *    <glowRadius>10</glowRadius><!--Just like vanilla-->
     *    <glowColor>(255,255,255,0)</glowColor><!--default color-->
     *    <key>YourMod_White</key><!--translation key to default color name-->
     *    <moreColors>
     *      <li><key>YourMod_Peach</key><color>(252,112,113,0)</color></li>
     *      <li><key>...</key><color>...</color></li>
     *    </moreColors>
     *    <!--<groupId>711712</groupId>------VERY optional-->
     *         <!--^^^^You can use it to group gizmos that
     *             have different color options, if wanted-->
     *  </li>
     * </comps>
     *
     * Enjoy!  --LWM
     *********************************************************************/
    public class CompProperties_Glower_ColorPick : CompProperties_Glower {
        public CompProperties_Glower_ColorPick()
		{
			this.compClass = typeof(CompGlower_ColorPick);
		}
        public override void ResolveReferences(ThingDef parentDef) {
            base.ResolveReferences(parentDef);
            // Use this opportunity to create a set of these compProperties, one for each color:
            colorComps=new List<CompProperties_Glower_ColorPick>();
            colorComps.Add(this);
            if (moreColors.NullOrEmpty()) return;
            foreach (var kc in moreColors) {
                CompProperties_Glower_ColorPick nextColor=new CompProperties_Glower_ColorPick() {
                    overlightRadius=this.overlightRadius,
                    glowRadius=this.glowRadius,
                    glowColor=kc.color,
                    key=kc.key,
                    colorComps=this.colorComps,
                };
                colorComps.Add(nextColor);
            }
        }
        public string key="default"; // translation key for adjective
        public List<KeyedColor> moreColors;
        public List<CompProperties_Glower_ColorPick> colorComps;
        // for multi-select:
        //   you can give different groupIds to objects that can turn different colors, or
        //   you can give them all the same and players will figure it out.  The code is
        //   flexible.
        public int groupId=711712;
    }
    public class KeyedColor {
        // Note: we do translation keys (not labels) so they can be used
        //  as an invariant save-data lookup
        public string key; // translation key for adjective
        public ColorInt color;
    }

    public class CompGlower_ColorPick : CompGlower {
        public new CompProperties_Glower_ColorPick Props {
            get {
                return (props as CompProperties_Glower_ColorPick);
            }
        }
        public override void PostExposeData() {
            base.PostExposeData();
            string origKey=Props.key;
            string defaultKey=Props.colorComps[0].key;
            string key=origKey;
            Scribe_Values.Look(ref key, "glower_color", defaultKey);
            if (key!=origKey) { // loaded new color
                ChangeColor(key);
            }
        }
        public void ChangeColor(string key) {
            if (key==Props.key) return;
            bool found=false;
            foreach(var c in Props.colorComps) {
                if (c.key==key) {
                    this.props=c;
                    found=true;
                    break;
                }
            }
            if (!found) {
                Log.Warning("CONFIG ERROR: could not find color "+key);
                return;
            }
            if (parent.Spawned) {
                parent.Map.glowGrid.DeRegisterGlower(this);
                parent.Map.glowGrid.RegisterGlower(this);
                //Log.Message(""+parent+" changing color to "+key);
            }
        }
        public void ChangeColorAllSelected(string key) {
            var selected=Find.Selector.SelectedObjects;
            if (selected.NullOrEmpty()) return;
            if (selected.Count < 2) return;
            foreach (object o in selected) {
                var c=(o as ThingWithComps)?.GetComp<CompGlower_ColorPick>();
                if (c==null) continue;
                foreach (var option in c.Props.colorComps) {
                    if (option.key==key) {
                        c.ChangeColor(key);
                        break; // stop testing options
                    }
                }
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra() {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;
            if (Props.colorComps==null || Props.colorComps.Count < 2) yield break;
            Color tmpColor=Props.glowColor.ToColor; // current color
            // don't blind anyone with bright icon:
            tmpColor.a=0.75f; // lowering "a" lowers how much color shows up

            yield return new Command_Action {
                defaultLabel="PRF_ChangeColorGizmo".Translate(this.Props.key.Translate()),
                //           (color)\nChange Color?
                defaultIconColor=tmpColor,
                groupKey=Props.groupId, // select multiple things at once
                icon=Texture2D.whiteTexture, // nice bright white background
                action=delegate() {
                    List<FloatMenuOption> mlist = new List<FloatMenuOption>();
                    foreach (var c in this.Props.colorComps) {
                        mlist.Add(new FloatMenuOption(c.key.Translate(),
                                                      delegate() {
                                                          this.ChangeColor(c.key);
                                                          ChangeColorAllSelected(c.key);
                                                      }));
                    }
                    Find.WindowStack.Add(new FloatMenu(mlist));
                }
            };
            yield break;
        }
    }
}
