using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;
using System.Linq;
using System.Diagnostics;

namespace InsertNameHere3.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration _configuration;
    private Plugin _plugin;
    private bool _waitingForKeyPress = false;
    private bool _keyPressInitialDelay = false;
    private DateTime _keyPressStartTime;

    public ConfigWindow(Plugin plugin) : base("你是坏孩子")
    {
        _configuration = plugin.Configuration;
        _plugin = plugin;
    }

    public void Dispose()
    {
    }

    // Helper method to display gray tip text with smaller font scale
    private void DrawGrayTipText(string text, float fontScale = 0.85f)
    {
        ImGui.PushFont(ImGui.GetIO().FontDefault);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
        ImGui.SetWindowFontScale(fontScale);
        if (text.Contains("\n") || text.Length > 80)
        {
            ImGui.TextWrapped(text);
        }
        else
        {
            ImGui.Text(text);
        }

        ImGui.SetWindowFontScale(1.0f);
        ImGui.PopStyleColor();
        ImGui.PopFont();
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("ConfigTabs", ImGuiTabBarFlags.Reorderable))
        {
            ImGui.PushItemWidth(450f);
            if (ImGui.BeginTabItem("Info"))
            {
                DrawInfoSettings();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("PvP"))
            {
                DrawPvPSettings();
                ImGui.EndTabItem();
            }
            //
            // //
            // if (ImGui.BeginTabItem("移動"))
            // {
            //     DrawMovementSettings();
            //     ImGui.EndTabItem();
            // }
            //
            // if (ImGui.BeginTabItem("新月島"))
            // {
            //     DrawOccultCrescentSettings();
            //     ImGui.EndTabItem();
            // }
            //
            // if (ImGui.BeginTabItem("死靈術士"))
            // {
            //     DrawnecroMancerSettings();
            //     ImGui.EndTabItem();
            // }
            //
            // if (ImGui.BeginTabItem("室內設計"))
            // {
            //     DrawInteriorDesignSettings();
            //     ImGui.EndTabItem();
            // }
            //
            // if (ImGui.BeginTabItem("Kisskiss"))
            // {
            //     DrawKisskissSettings();
            //     ImGui.EndTabItem();
            // }
            //
            // if (ImGui.BeginTabItem("多變迷宮"))
            // {
            //     DrawMultiDungeonSettings();
            //     ImGui.EndTabItem();
            // }
            //
            // if (ImGui.BeginTabItem("親信戰友"))
            // {
            //     DrawFaithSettings();
            //     ImGui.EndTabItem();
            // }
            //
            // if (ImGui.BeginTabItem("Teleporter"))
            // {
            //     DrawTeleporterManagerSettings();
            //     ImGui.EndTabItem();
            // }
            //
            // if (ImGui.BeginTabItem("色色"))
            // {
            //     DrawEroSettings();
            //     ImGui.EndTabItem();
            // }
            //
            // if (ImGui.BeginTabItem("抢房"))
            // {
            //     DrawHousingSettings();
            //     ImGui.EndTabItem();
            // }

            if (ImGui.BeginTabItem("Debug"))
            {
                DrawDebugSettings();
                ImGui.EndTabItem();
            }

            ImGui.PopItemWidth();
            ImGui.EndTabBar();
        }
    }
    
    private void DrawPvPSettings()
    {
        // PvP Settings Section
        if (ImGui.CollapsingHeader("设定", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            ImGui.Separator();
            ImGui.Text("距离计算修正");
            var compatibleDistanceCalculation = _configuration.CompatibleDistanceCalculation;
            if (ImGui.Checkbox("兼容距离计算", ref compatibleDistanceCalculation))
            {
                _configuration.CompatibleDistanceCalculation = compatibleDistanceCalculation;
                _configuration.Save();
            }

            DrawGrayTipText("如果需要自定义触发距离, 或者使用了各类增加施法距离的插件后工作不正常, 请启用该选项. 如果使用了其他施法距离增加插件,障碍物遮挡检查可能失效.");

            var comboStartDistanceCorrection = _configuration.ComboStartDistanceCorrection;
            if (ImGui.SliderInt("连击启动距离补正", ref comboStartDistanceCorrection, -25, 10))
            {
                _configuration.ComboStartDistanceCorrection = comboStartDistanceCorrection;
                _configuration.Save();
            }

            DrawGrayTipText($"假设连击最小距离要求 25m, 当距离目标 {25 + comboStartDistanceCorrection}m 的时候开始连击");

            var comboFollowUpAndSingleSkillDistanceCorrection =
                _configuration.ComboFollowUpAndSingleSkillDistanceCorrection;
            if (ImGui.SliderInt("连击后续与单技能距离补正", ref comboFollowUpAndSingleSkillDistanceCorrection, -25, 10))
            {
                _configuration.ComboFollowUpAndSingleSkillDistanceCorrection =
                    comboFollowUpAndSingleSkillDistanceCorrection;
                _configuration.Save();
            }

            DrawGrayTipText($"假设技能距离 25m, 当距离目标 {25 + comboFollowUpAndSingleSkillDistanceCorrection}m 的时候尝试执行技能");
            ImGui.Separator();
            ImGui.Text("自动技能与自动选中职业设定");
            if (ImGui.CollapsingHeader("允许作为目标的职业:", ImGuiTreeNodeFlags.None))
            {
                ImGui.Indent();
                // Tank jobs (light blue)
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.8f, 1.0f, 1.0f));
                DrawJobCheckbox("骑士", InsertNameHere3.Service.JobPaladin);
                DrawJobCheckbox("战士", InsertNameHere3.Service.JobWarrior);
                DrawJobCheckbox("暗黑骑士", InsertNameHere3.Service.JobDarkKnight);
                DrawJobCheckbox("绝枪战士", InsertNameHere3.Service.JobGunBlade);
                ImGui.PopStyleColor();

                // Healer jobs (light green)
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 1.0f, 0.5f, 1.0f));
                DrawJobCheckbox("白魔法师", InsertNameHere3.Service.JobWhiteMage);
                DrawJobCheckbox("学者", InsertNameHere3.Service.JobScholar);
                DrawJobCheckbox("占星术士", InsertNameHere3.Service.JobAstrologian);
                DrawJobCheckbox("贤者", InsertNameHere3.Service.JobSage);
                ImGui.PopStyleColor();

                // DPS jobs (light red)
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.5f, 0.5f, 1.0f));
                DrawJobCheckbox("武僧", InsertNameHere3.Service.JobMonk);
                DrawJobCheckbox("龙骑士", InsertNameHere3.Service.JobDragoon);
                DrawJobCheckbox("忍者", InsertNameHere3.Service.JobNinja);
                DrawJobCheckbox("武士", InsertNameHere3.Service.JobSamurai);
                DrawJobCheckbox("钐镰客", InsertNameHere3.Service.JobReaper);
                DrawJobCheckbox("蝰蛇剑士", InsertNameHere3.Service.JobViper);
                DrawJobCheckbox("吟游诗人", InsertNameHere3.Service.JobBard);
                DrawJobCheckbox("机工士", InsertNameHere3.Service.JobMachinist);
                DrawJobCheckbox("舞者", InsertNameHere3.Service.JobDancer);
                DrawJobCheckbox("黑魔法师", InsertNameHere3.Service.JobBlackMage);
                DrawJobCheckbox("召唤师", InsertNameHere3.Service.JobSummoner);
                DrawJobCheckbox("赤魔法师", InsertNameHere3.Service.JobRedMage);
                DrawJobCheckbox("绘灵法师", InsertNameHere3.Service.JobPictoMancer);
                ImGui.PopStyleColor();
                ImGui.Unindent();
            }

            ImGui.Separator();
            ImGui.Text("自动输出血量设定");
            var autoSequenceOverCap = _configuration.AutoSequenceOverCap;
            var autoSequenceMinFold = _configuration.AutoSequenceMinFold;
            int[] autoSequenceRange = [autoSequenceMinFold, autoSequenceOverCap];
            if (ImGui.SliderInt2("血量补正", ref autoSequenceRange[0], 0, 200))
            {
                if (autoSequenceRange[0] > autoSequenceRange[1])
                {
                    autoSequenceRange[1] = autoSequenceRange[0];
                }

                if (autoSequenceRange[1] < autoSequenceRange[0])
                {
                    autoSequenceRange[0] = autoSequenceRange[1];
                }

                _configuration.AutoSequenceMinFold = autoSequenceRange[0];
                _configuration.AutoSequenceOverCap = autoSequenceRange[1];
                _configuration.Save();
            }

            DrawGrayTipText(
                $"当目标血量为预期伤害 {((float)autoSequenceMinFold / 100).ToString("P0")}% ~ {((float)autoSequenceOverCap / 100).ToString("P0")}% 时执行\n假设预期伤害 40000, 则目标血量为{Math.Round((float)autoSequenceMinFold / 100 * 40000)} ~ {Math.Round((float)autoSequenceOverCap / 100 * 40000)} 时执行");

            var expectedDamageBuffCalculation = _configuration.ExpectedDamageBuffCalculation;
            if (ImGui.Checkbox("预期伤害/目标选择计算buff, 于战场时同时计算职业补正", ref expectedDamageBuffCalculation))
            {
                _configuration.ExpectedDamageBuffCalculation = expectedDamageBuffCalculation;
                _configuration.Save();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text("自动技能触发热键");
            ImGui.TextColored(new System.Numerics.Vector4(1f, 1f, 0f, 1f),
                $"自动技能状态: {(_configuration.PvPAutoSkillsEnabled ? "启用" : "禁用")}");

            // Toggle Mode Selection
            // ImGui.Text("切换模式:");
            // ImGui.SameLine();

            var modeNames = new[] { "禁用热键", "按住时启用", "按住时禁用", "切换" };
            var currentMode = (int)_configuration.PvPToggleMode;

            if (ImGui.Combo("触发模式", ref currentMode, modeNames, modeNames.Length))
            {
                _configuration.PvPToggleMode = (PvPToggleMode)currentMode;
                _configuration.Save();

                // Reset to default state when changing modes
                if (_configuration.PvPToggleMode == PvPToggleMode.Disabled)
                {
                    _configuration.PvPAutoSkillsEnabled = true; // Always enabled when fast toggle is disabled
                }
                else if (_configuration.PvPToggleMode == PvPToggleMode.EnableOnPress)
                {
                    _configuration.PvPAutoSkillsEnabled = false; // Default disabled, enable on press
                    _configuration.PvPAutoSkillsDefaultState = false;
                }
                else if (_configuration.PvPToggleMode == PvPToggleMode.DisableOnPress)
                {
                    _configuration.PvPAutoSkillsEnabled = true; // Default enabled, disable on press
                    _configuration.PvPAutoSkillsDefaultState = true;
                }
            }

            ImGui.Spacing();

            // Only show hotkey setting if not disabled
            if (_configuration.PvPToggleMode != PvPToggleMode.Disabled)
            {
                var keyName = _configuration.PvPAutoSkillsToggleKey == 0
                    ? "未设置"
                    : ((ECommons.Interop.LimitedKeys)_configuration.PvPAutoSkillsToggleKey).ToString();

                if (ImGui.Button($"当前热键: {keyName}##PvPToggleKey"))
                {
                    _waitingForKeyPress = true;
                    _keyPressInitialDelay = true;
                    _keyPressStartTime = DateTime.Now;
                    ImGui.OpenPopup("SetPvPToggleKey");
                }

                if (ImGui.BeginPopup("SetPvPToggleKey"))
                {
                    ImGui.Text("按下想要设定的按键:");
                    ImGui.Separator();

                    // Wait a bit to avoid capturing the mouse click that opened the popup
                    if (_keyPressInitialDelay && (DateTime.Now - _keyPressStartTime).TotalMilliseconds < 500)
                    {
                        ImGui.Text("请稍候...");
                    }
                    else
                    {
                        _keyPressInitialDelay = false;
                        ImGui.TextColored(new Vector4(1f, 1f, 0f, 1f), "等待按键输入...");

                        var validKeys = Enum.GetValues<ECommons.Interop.LimitedKeys>()
                            .Where(key => key > ECommons.Interop.LimitedKeys.RightMouseButton &&
                                          (key < ECommons.Interop.LimitedKeys.KanaMode ||
                                           key > ECommons.Interop.LimitedKeys.IMEModeChange)) // Exclude None
                            .ToArray();

                        // Check for key presses
                        foreach (var key in validKeys)
                        {
                            if (ECommons.GenericHelpers.IsKeyPressed(key))
                            {
                                _configuration.PvPAutoSkillsToggleKey = (int)key;
                                _configuration.Save();
                                _waitingForKeyPress = false;
                                ImGui.CloseCurrentPopup();
                                break;
                            }
                        }
                    }

                    ImGui.Spacing();
                    ImGui.Separator();

                    if (ImGui.Button("消除热键"))
                    {
                        _configuration.PvPAutoSkillsToggleKey = 0;
                        _configuration.Save();
                        _waitingForKeyPress = false;
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("取消"))
                    {
                        _waitingForKeyPress = false;
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.EndPopup();
                }
                else
                {
                    // Reset state if popup is closed
                    _waitingForKeyPress = false;
                    _keyPressInitialDelay = false;
                }
            }


            ImGui.Unindent();
        }

        // Targeting Settings Card
        if (ImGui.CollapsingHeader("目标选择", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            var enableAutoSelect = _configuration.EnableAutoSelect;
            if (ImGui.Checkbox("自动选中", ref enableAutoSelect))
            {
                _configuration.EnableAutoSelect = enableAutoSelect;
                _configuration.Save();
            }
            DrawGrayTipText("仅仅为「你」选择目标, 不影响自动技能");

            var targetingRange = _configuration.TargetingRange;
            if (ImGui.SliderInt("选中距离", ref targetingRange, 10, 50))
            {
                _configuration.TargetingRange = targetingRange;
                _configuration.Save();
            }

            var onlyTarget50 = _configuration.OnlyTarget50;
            if (ImGui.Checkbox("仅选中低于50%生命值", ref onlyTarget50))
            {
                _configuration.OnlyTarget50 = onlyTarget50;
                _configuration.Save();
            }

            // var selectDrkKnt = Configuration.ExcludeDrkAndKnight;
            // if (ImGui.Checkbox("排除騎士黑騎", ref selectDrkKnt))
            // {
            //     Configuration.ExcludeDrkAndKnight = selectDrkKnt;
            //     Configuration.Save();
            // }

            var exludeGuard = _configuration.ExcludeBeingProtected;
            if (ImGui.Checkbox("不选中无敌的对象", ref exludeGuard))
            {
                _configuration.ExcludeBeingProtected = exludeGuard;
                _configuration.Save();
            }

            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("自保", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            var autoBubble = _configuration.AutoBubble;
            if (ImGui.Checkbox("被机工LB自动泡泡", ref autoBubble))
            {
                _configuration.AutoBubble = autoBubble;
                _configuration.Save();
            }

            var autoBubbleBlcok = _configuration.AutoBubbleBlock;
            if (ImGui.Checkbox("自动泡泡2s防误触", ref autoBubbleBlcok))
            {
                _configuration.AutoBubbleBlock = autoBubbleBlcok;
                _configuration.Save();
            }

            var enableAutoPurify = _configuration.AutoPurify;
            if (ImGui.Checkbox("自动康复", ref enableAutoPurify))
            {
                _configuration.AutoPurify = enableAutoPurify;
                _configuration.Save();
            }
            
            // Individual auto purify debuff settings - collapsible
            if (_configuration.AutoPurify && ImGui.CollapsingHeader("康复设定", ImGuiTreeNodeFlags.None))
            {
                ImGui.Indent();
                // Human Reaction Section
                if (ImGui.CollapsingHeader("人类反应", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Indent();
                    // Define human reaction debuff types
                    var humanReactionDebuffs = new (uint id, string name)[]
                    {
                        (InsertNameHere3.Service.Buff_Stun, "眩晕"),
                        (InsertNameHere3.Service.Buff_Heavy, "加重"),
                        (InsertNameHere3.Service.Buff_Bind, "止步"),
                        (InsertNameHere3.Service.Buff_Silence, "沉默"),
                        (InsertNameHere3.Service.Buff_Sleep, "睡眠"),
                        (InsertNameHere3.Service.Buff_HalfAsleep, "缓缓入睡"),
                        (InsertNameHere3.Service.Buff_DeepFreeze, "冰冻"),
                        (InsertNameHere3.Service.Buff_AmazingNature, "变猪")
                    };
                    
                    // Create checkboxes for human reaction debuffs
                    foreach (var (id, name) in humanReactionDebuffs)
                    {
                        var isEnabled = _configuration.AutoPurifyHumanReaction.Contains(id);
                        if (ImGui.Checkbox($"{name}##human", ref isEnabled))
                        {
                            if (isEnabled)
                                _configuration.AutoPurifyHumanReaction.Add(id);
                            else
                                _configuration.AutoPurifyHumanReaction.Remove(id);
                            _configuration.Save();
                        }
                    }
                    
                    ImGui.Unindent();
                }
                
                // Neuro Speed Section
                if (ImGui.CollapsingHeader("Neuralink", ImGuiTreeNodeFlags.None))
                {
                    ImGui.Indent();
                    
                    // Neuro link button
                    if (ImGui.Button("You must be Neuro to use these"))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://space.bilibili.com/3546729368520811")
                        {
                            UseShellExecute = true
                        });
                    }
                    
                    ImGui.Spacing();

                    // Blota reaction checkbox
                    var blotaReaction = _configuration.AutoPurifyBlotaReaction;
                    if (ImGui.Checkbox("战士死斗", ref blotaReaction))
                    {
                        _configuration.AutoPurifyBlotaReaction = blotaReaction;
                        _configuration.Save();
                    }
                    
                    // WindsReply reaction checkbox
                    var windsReplyReaction = _configuration.AutoPurifyWindsReplyReaction;
                    if (ImGui.Checkbox("武僧绝空拳", ref windsReplyReaction))
                    {
                        _configuration.AutoPurifyWindsReplyReaction = windsReplyReaction;
                        _configuration.Save();
                    }
                    
                    ImGui.Unindent();
                }
                
                ImGui.Unindent();
            }

            var enableAutoElixir = _configuration.AutoElixir;
            if (ImGui.Checkbox("自动自愈", ref enableAutoElixir))
            {
                _configuration.AutoElixir = enableAutoElixir;
                _configuration.Save();
            }

            var autoElixirPercentage = _configuration.AutoElixirPercentage;
            if (ImGui.SliderInt("触发生命%", ref autoElixirPercentage, 1, 100))
            {
                _configuration.AutoElixirPercentage = autoElixirPercentage;
                _configuration.Save();
            }

            var disablePurifyWhenSelfGuard = _configuration.DisableCureWhenSelfGuard;
            if (ImGui.Checkbox("泡泡时不使用自动自愈", ref disablePurifyWhenSelfGuard))
            {
                _configuration.DisableCureWhenSelfGuard = disablePurifyWhenSelfGuard;
                _configuration.Save();
            }

            ImGui.Unindent();
        }

        // Job-specific Settings Card
        if (ImGui.CollapsingHeader("职业专用设定", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            // Monk Settings
            ImGui.Text("武僧");

            var monkAutoSmite = _configuration.MonkAutoSmite;
            if (ImGui.Checkbox("自动猛击##monk", ref monkAutoSmite))
            {
                _configuration.MonkAutoSmite = monkAutoSmite;
                _configuration.Save();
            }

            var monkAutoEarthsReply = _configuration.MonkAutoEarthsReply;
            if (ImGui.Checkbox("自动金刚转轮", ref monkAutoEarthsReply))
            {
                _configuration.MonkAutoEarthsReply = monkAutoEarthsReply;
                _configuration.Save();
            }

            ImGui.SameLine();
            ImGui.Text("触发时机:");
            var monkAutoEarthsReplyTiming = _configuration.MonkAutoEarthsReplyTiming;
            if (ImGui.SliderFloat("剩余时间(秒)", ref monkAutoEarthsReplyTiming, 0.1f, 2.5f, "%.1f"))
            {
                _configuration.MonkAutoEarthsReplyTiming = monkAutoEarthsReplyTiming;
                _configuration.Save();
            }

            ImGui.Spacing();
            ImGui.Separator();
            // Dragoon Settings
            ImGui.Text("龙骑士");

            var dragoonAutoSmite = _configuration.DragoonAutoSmite;
            if (ImGui.Checkbox("自动猛击##dragoon", ref dragoonAutoSmite))
            {
                _configuration.DragoonAutoSmite = dragoonAutoSmite;
                _configuration.Save();
            }

            var dragoonForwardJump = _configuration.DragoonForwardJump;
            if (ImGui.Checkbox("冲锋跳跃(experimental)##dragoonForwardJump", ref dragoonForwardJump))
            {
                _configuration.DragoonForwardJump = dragoonForwardJump;
                _configuration.Save();
            }

            ImGui.Spacing();
            ImGui.Separator();
            // Ninja Settings  
            ImGui.Text("忍者");

            var ninjaAutoSmite = _configuration.NinjaAutoSmite;
            if (ImGui.Checkbox("自动猛击##ninja", ref ninjaAutoSmite))
            {
                _configuration.NinjaAutoSmite = ninjaAutoSmite;
                _configuration.Save();
            }

            var ninAutoLB = _configuration.NinjaAutoLB;
            if (ImGui.Checkbox("自动星遁天诛", ref ninAutoLB))
            {
                _configuration.NinjaAutoLB = ninAutoLB;
                _configuration.Save();
            }

            ImGui.Spacing();
            ImGui.Separator();
            // Samurai Settings
            ImGui.Text("武士");

            var samuraiAutoSmite = _configuration.SamuraiAutoSmite;
            if (ImGui.Checkbox("自动猛击##samurai", ref samuraiAutoSmite))
            {
                _configuration.SamuraiAutoSmite = samuraiAutoSmite;
                _configuration.Save();
            }

            var samAutoLB = _configuration.SamuraiAutoLB;
            if (ImGui.Checkbox("自动斩铁剑", ref samAutoLB))
            {
                _configuration.SamuraiAutoLB = samAutoLB;
                _configuration.Save();
            }

            var samuraiAutoLBMinAmount = _configuration.SamuraiAutoLBMinAmount;
            ImGui.SetNextItemWidth(100);
            if (ImGui.SliderInt("同时斩人##samurai", ref samuraiAutoLBMinAmount, 1, 5))
            {
                _configuration.SamuraiAutoLBMinAmount = samuraiAutoLBMinAmount;
                _configuration.Save();
            }

            var samuraiAutoLBAllowNonWeak = _configuration.SamuraiAutoLBAllowNonWeak;
            if (ImGui.Checkbox("可选中未崩破但LB可斩更多人的目标", ref samuraiAutoLBAllowNonWeak))
            {
                _configuration.SamuraiAutoLBAllowNonWeak = samuraiAutoLBAllowNonWeak;
                _configuration.Save();
            }

            DrawGrayTipText("7.1后武士判定延迟极高, 请仅斩1免于落泪");

            ImGui.Spacing();
            ImGui.Separator();
            // Reaper Settings
            ImGui.Text("钐镰客");

            var reaperAutoSmite = _configuration.ReaperAutoSmite;
            if (ImGui.Checkbox("自动猛击##reaper", ref reaperAutoSmite))
            {
                _configuration.ReaperAutoSmite = reaperAutoSmite;
                _configuration.Save();
            }

            var reaperAutoPerfectio = _configuration.ReaperAutoPerfectio;
            if (ImGui.Checkbox("自动完人##reaper", ref reaperAutoPerfectio))
            {
                _configuration.ReaperAutoPerfectio = reaperAutoPerfectio;
                _configuration.Save();
            }
            
            var reaperAutoPerfectioMinPredictionKill = _configuration.ReaperAutoPerfectioMinPredictionKill;
            ImGui.SetNextItemWidth(100);
            if (ImGui.SliderInt("同时斩人##reaper", ref reaperAutoPerfectioMinPredictionKill, 1, 5))
            {
                _configuration.ReaperAutoPerfectioMinPredictionKill = reaperAutoPerfectioMinPredictionKill;
                _configuration.Save();
            }
            var reaperAutoPerfectioAllowNonWeak = _configuration.ReaperAutoPerfectioAllowNonWeak;
            if (ImGui.Checkbox("可选中未残血但完人可斩更多人的目标##reaper", ref reaperAutoPerfectioAllowNonWeak))
            {
                _configuration.ReaperAutoPerfectioAllowNonWeak = reaperAutoPerfectioAllowNonWeak;
                _configuration.Save();
            }

            DrawGrayTipText("完人的伤害延迟比7.1后斩铁剑还久, 放开希望免于落泪");

            ImGui.Spacing();
            ImGui.Separator();
            // Viper Settings
            ImGui.Text("蝰蛇剑士");

            var viperAutoSmite = _configuration.ViperAutoSmite;
            if (ImGui.Checkbox("自动猛击##viper", ref viperAutoSmite))
            {
                _configuration.ViperAutoSmite = viperAutoSmite;
                _configuration.Save();
            }

            var viperAutoSerpentsTail = _configuration.ViperAutoSerpentsTail;
            if (ImGui.Checkbox("自动续剑##viper", ref viperAutoSerpentsTail))
            {
                _configuration.ViperAutoSerpentsTail = viperAutoSerpentsTail;
                _configuration.Save();
            }
            DrawGrayTipText("对当前目标释放, 会打冰和无人机, 如果超过1s没有打出续剑会放弃, 请靠近一点敌人");

            ImGui.Spacing();
            ImGui.Separator();
            // Machinist Settings
            ImGui.Text("机工");

            var mchAutoLB = _configuration.MachinistAutoLB;
            if (ImGui.Checkbox("机工自动LB", ref mchAutoLB))
            {
                _configuration.MachinistAutoLB = mchAutoLB;
                _configuration.Save();
            }

            var mchAutoEE = _configuration.MachinistAutoEagleEye;
            if (ImGui.Checkbox("机工自动锐眼", ref mchAutoEE))
            {
                _configuration.MachinistAutoEagleEye = mchAutoEE;
                _configuration.Save();
            }

            var mchAutoWF = _configuration.MachinistAutoWildFire;
            if (ImGui.Checkbox("机工自动野火连击/请携带锐眼", ref mchAutoWF))
            {
                _configuration.MachinistAutoWildFire = mchAutoWF;
                _configuration.Save();
            }

            var mchAutoWFUseLB = _configuration.MachinistAutoWildFireMayUseLB;
            if (ImGui.Checkbox("允许野火使用LB", ref mchAutoWFUseLB))
            {
                _configuration.MachinistAutoWildFireMayUseLB = mchAutoWFUseLB;
                _configuration.Save();
            }


            // Show current available combo status
            DrawGrayTipText("已启用触发:");

            // Show status based on enabled options
            if (_configuration.MachinistAutoLB && _configuration.MachinistAutoEagleEye)
            {
                DrawGrayTipText("自动LB+锐眼, 预期52000");
            }

            if (_configuration.MachinistAutoLB)
            {
                DrawGrayTipText("自动LB, 预期40000");
            }

            if (_configuration.MachinistAutoEagleEye)
            {
                DrawGrayTipText("自动锐眼, 预期12000穿盾");
            }

            if (_configuration.MachinistAutoWildFire)
            {
                if (_configuration.MachinistAutoWildFireMayUseLB)
                {
                    DrawGrayTipText("野火全金属锐眼LB, 预期83000");
                }

                DrawGrayTipText("野火[钻头/空气锚/链锯]锐眼全金属, 预期[61000/55000/63600]");
            }

            ImGui.Spacing();

            var avoidBubbleEnemiesForAutoLB = _configuration.AvoidBubbleEnemiesForAutoLB;
            if (ImGui.Checkbox("不对泡泡未冷却的敌人释放LB/仅55生效", ref avoidBubbleEnemiesForAutoLB))
            {
                _configuration.AvoidBubbleEnemiesForAutoLB = avoidBubbleEnemiesForAutoLB;
                _configuration.Save();
            }

            ImGui.Spacing();
            ImGui.Separator();
            // Bard Settings
            ImGui.Text("吟游诗人");

            var bardAutoEE = _configuration.BardAutoEagleEye;
            if (ImGui.Checkbox("自动锐眼##bard", ref bardAutoEE))
            {
                _configuration.BardAutoEagleEye = bardAutoEE;
                _configuration.Save();
            }

            var bardAutoHarmonic = _configuration.BardAutoHarmonicArrow;
            if (ImGui.Checkbox("自动和弦箭##bard", ref bardAutoHarmonic))
            {
                _configuration.BardAutoHarmonicArrow = bardAutoHarmonic;
                _configuration.Save();
            }
            
            DrawGrayTipText("已启用触发:");

            // Show current combo status based on settings
            if (_configuration.BardAutoEagleEye && _configuration.BardAutoHarmonicArrow)
            {
                DrawGrayTipText("和弦箭锐眼, 预期30000");
            }
            if (_configuration.BardAutoEagleEye)
            {
                DrawGrayTipText("自动锐眼, 预期12000");
            }

            ImGui.Spacing();
            ImGui.Separator();
            // Scholar Settings
            ImGui.Text("学者");

            var scholarAutoSpreadPoison = _configuration.ScholarAutoSpreadPoison;
            if (ImGui.Checkbox("自动毒", ref scholarAutoSpreadPoison))
            {
                _configuration.ScholarAutoSpreadPoison = scholarAutoSpreadPoison;
                _configuration.Save();
            }

            var scholarSecretTacticsMode = _configuration.ScholarSecretTacticsMode;
            string[] secretTacticsModes = ["自动秘策 - 仅秘策时扩毒", "手动秘策 - 仅秘策时扩毒", "毒可用即扩 - 尝试自动秘策", "毒可用即扩"];
            if (ImGui.Combo("秘策", ref scholarSecretTacticsMode, secretTacticsModes, secretTacticsModes.Length))
            {
                _configuration.ScholarSecretTacticsMode = scholarSecretTacticsMode;
                _configuration.Save();
            }

            var scholarSpreadPoisonTargetCount = _configuration.ScholarSpreadPoisonTargetCount;
            if (ImGui.SliderInt("扩毒人数", ref scholarSpreadPoisonTargetCount, 2, 30))
            {
                _configuration.ScholarSpreadPoisonTargetCount = scholarSpreadPoisonTargetCount;
                _configuration.Save();
            }


            DrawGrayTipText("扩毒视为单技能, 遵循单技能释放距离设定\n毒伤为主目标快照, 会尝试选取人数不是最多但总伤害最高的目标");

            ImGui.Spacing();
            ImGui.Separator();
            // Warrior Settings
            ImGui.Text("战士");

            var warriorPrimalRendTargetCorrection = _configuration.WarriorPrimalRendTargetCorrection;
            string[] primalRendModes = ["不修正", "当前面向120度内", "范围内任意"];
            if (ImGui.Combo("蛮荒崩裂目标修正", ref warriorPrimalRendTargetCorrection, primalRendModes,
                    primalRendModes.Length))
            {
                _configuration.WarriorPrimalRendTargetCorrection = warriorPrimalRendTargetCorrection;
                _configuration.Save();
            }

            var warriorPrimalScreamTargetCorrection = _configuration.WarriorPrimalScreamTargetCorrection;
            if (ImGui.Checkbox("原初的怒号(LB)目标修正", ref warriorPrimalScreamTargetCorrection))
            {
                _configuration.WarriorPrimalScreamTargetCorrection = warriorPrimalScreamTargetCorrection;
                _configuration.Save();
            }

            DrawGrayTipText("自动选择能击中最多敌人的目标位置");
            ImGui.Spacing();
            ImGui.Separator();
            // Dark Knight Settings
            ImGui.Text("暗黑骑士");

            var darkKnightPlungeTargetCorrection = _configuration.DarkKnightPlungeTargetCorrection;
            string[] plungeModes = ["不修正", "当前面向120度内", "范围内任意"];
            if (ImGui.Combo("跳斩(腐秽大地)目标修正", ref darkKnightPlungeTargetCorrection, plungeModes, plungeModes.Length))
            {
                _configuration.DarkKnightPlungeTargetCorrection = darkKnightPlungeTargetCorrection;
                _configuration.Save();
            }

            DrawGrayTipText("自动选择能击中最多敌人的目标位置");
            ImGui.Spacing();
            ImGui.Separator();

            ImGui.Unindent();
        }

        // Target Icon Settings (Cooldown Tracking)
        if (ImGui.CollapsingHeader("状态追踪/仅55", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            var showTargetIcon = _configuration.ShowTargetIcon;
            if (ImGui.Checkbox("显示敌人技能冷却", ref showTargetIcon))
            {
                _configuration.ShowTargetIcon = showTargetIcon;
                _configuration.Save();
            }

            DrawGrayTipText("防御, 净化");

            if (_configuration.ShowTargetIcon)
            {
                var showTeammateCooldowns = _configuration.ShowTeammateCooldowns;
                if (ImGui.Checkbox("显示队友技能冷却", ref showTeammateCooldowns))
                {
                    _configuration.ShowTeammateCooldowns = showTeammateCooldowns;
                    _configuration.Save();
                }

                DrawGrayTipText("在队友头上显示技能冷却（绿色）");
            }

            var showEnemyWatchers = _configuration.ShowEnemyWatchers;
            if (ImGui.Checkbox("显示目标线", ref showEnemyWatchers))
            {
                _configuration.ShowEnemyWatchers = showEnemyWatchers;
                _configuration.Save();
            }

            DrawGrayTipText("在注视你的敌人和你之间画线");
            
            // Inline line color customization for enemy watchers
            if (_configuration.ShowEnemyWatchers)
            {
                var drawList = ImGui.GetWindowDrawList();
                var previewSize = new Vector2(30, 15);
                
                // Enemy watching player line color
                var enemyWatchColor = _configuration.EnemyWatcherLineColor;
                ImGui.Text("敌人注视线颜色:");
                ImGui.SameLine();
                if (ImGui.ColorEdit4("##EnemyWatcherLine", ref enemyWatchColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview))
                {
                    _configuration.EnemyWatcherLineColor = enemyWatchColor;
                    _configuration.Save();
                }
                ImGui.SameLine();
                var cursorPos1 = ImGui.GetCursorScreenPos();
                drawList.AddRectFilled(cursorPos1, cursorPos1 + previewSize, ImGui.ColorConvertFloat4ToU32(enemyWatchColor));
                drawList.AddRect(cursorPos1, cursorPos1 + previewSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f)));
                ImGui.Dummy(previewSize);
                
                // Player targeting enemy line color
                var playerTargetColor = _configuration.PlayerTargetLineColor;
                ImGui.Text("你的锁定线颜色:");
                ImGui.SameLine();
                if (ImGui.ColorEdit4("##PlayerTargetLine", ref playerTargetColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview))
                {
                    _configuration.PlayerTargetLineColor = playerTargetColor;
                    _configuration.Save();
                }
                ImGui.SameLine();
                var cursorPos2 = ImGui.GetCursorScreenPos();
                drawList.AddRectFilled(cursorPos2, cursorPos2 + previewSize, ImGui.ColorConvertFloat4ToU32(playerTargetColor));
                drawList.AddRect(cursorPos2, cursorPos2 + previewSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f)));
                ImGui.Dummy(previewSize);
            }

            var showTeammateWatchers = _configuration.ShowTeammateWatchers;
            if (ImGui.Checkbox("显示队友受击目标线", ref showTeammateWatchers))
            {
                _configuration.ShowTeammateWatchers = showTeammateWatchers;
                _configuration.Save();
            }

            DrawGrayTipText("在注视队友的敌人和队友之间画线（橙色）");
            
            // Inline line color customization for teammate watchers
            if (_configuration.ShowTeammateWatchers)
            {
                var drawList = ImGui.GetWindowDrawList();
                var previewSize = new Vector2(30, 15);
                
                // Teammate being watched line color
                var teammateWatchedColor = _configuration.TeammateWatchedLineColor;
                ImGui.Text("队友受击线颜色:");
                ImGui.SameLine();
                if (ImGui.ColorEdit4("##TeammateWatchedLine", ref teammateWatchedColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview))
                {
                    _configuration.TeammateWatchedLineColor = teammateWatchedColor;
                    _configuration.Save();
                }
                ImGui.SameLine();
                var cursorPos3 = ImGui.GetCursorScreenPos();
                drawList.AddRectFilled(cursorPos3, cursorPos3 + previewSize, ImGui.ColorConvertFloat4ToU32(teammateWatchedColor));
                drawList.AddRect(cursorPos3, cursorPos3 + previewSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f)));
                ImGui.Dummy(previewSize);
            }
            
            // Reset all colors button at the end
            ImGui.Spacing();
            if (ImGui.Button("重置所有颜色为预设"))
            {
                _configuration.TargetIconEnemyColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                _configuration.TargetIconTeammateColor = new Vector4(0.5f, 1.0f, 0.5f, 1.0f);
                _configuration.EnemyWatcherLineColor = new Vector4(1.0f, 0.2f, 0.2f, 0.8f);
                _configuration.PlayerTargetLineColor = new Vector4(0.2f, 1.0f, 0.2f, 0.8f);
                _configuration.TeammateWatchedLineColor = new Vector4(1.0f, 0.5f, 0.0f, 0.8f);
                _configuration.Save();
            }
            
            ImGui.Unindent();
        }
    }

    private void DrawJobCheckbox(string jobName, uint jobId)
    {
        if (!_configuration.AllowedTargetJobs.ContainsKey(jobId))
        {
            _configuration.AllowedTargetJobs[jobId] = false;
        }

        var isAllowed = _configuration.AllowedTargetJobs[jobId];
        if (ImGui.Checkbox(jobName, ref isAllowed))
        {
            _configuration.AllowedTargetJobs[jobId] = isAllowed;
            _configuration.Save();
        }
    }

    
    private void DrawInfoSettings()
    {
        // Version Header
        ImGui.TextColored(new Vector4(1, 1, 0, 1), "7.2.0.18 仅PVP");
        ImGui.Spacing();

        // Question 1
        if (ImGui.CollapsingHeader("1. 这是什么", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            ImGui.TextWrapped("这个版本战场百鬼夜行群魔乱舞, 邀诸君体验, 祝战场愉快");
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("2. 有哪些已知问题", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            ImGui.TextWrapped("机工连击与学者毒会强制改变面向, 可能不适合标准移动方式");
            ImGui.TextWrapped("如果你会写好一点的技能触发方式, 教我{>ω<♡*");
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("3. 常见问题", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            ImGui.TextWrapped("a. 忍者/机工会LB大冰, 无人机, 连击只会打第一下, 或者每次攻击之间会停顿数秒: 可能开了WC, SX或者其他自动输出插件, 最好不要和其他自动输出插件同时使用");
            ImGui.TextWrapped(
                "b. 机工会尝试瞄准射程外的玩家, 忍者LB/战士冲锋会打特别远距离的目标, 一直尝试放技能但放不出来导致无法移动: 可能开了增加施法距离, 冲锋无视障碍, 施法无视阻挡等插件, 请启用兼容距离计算并善用临时禁用按键.");
            ImGui.TextWrapped("c. 头上有冷却计时不消除, 自动攻击不会触发或对满血目标使用: 不要打开 debug");
            ImGui.Spacing();
            ImGui.TextWrapped("若有其他问题劳烦先关闭其他插件, 特别是WrathCombo和DailyRoutine, 检查一下是否还会出现. 若依然出现或冲突插件较重要想兼容请自己动手丰衣足食");
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("4. 这大概是最后一次更新", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            ImGui.TextWrapped("月卡用完啦; 也正失去兴趣; 自己还玻璃心, 无论是 qq, discord 或 github issue 都有人直接有罪推定论处; 爆了!");
            if (ImGui.Button("按这里查看所有使用者信息"))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/transparent-duck/InsertNameHere3/blob/main/fakeUserList.md")
                {
                    UseShellExecute = true
                });
            }
            

            ImGui.TextWrapped("如若您有兴趣, 您可以自己修改构建. 请遵循 AGPL 3.0 license.");
            if (ImGui.Button("按这里查看原始码"))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/transparent-duck/InsertNameHere3")
                {
                    UseShellExecute = true
                });
            }

            ImGui.TextWrapped("");
            ImGui.Unindent();
        }
    }

    private void DrawDebugSettings()
    {
        var debug = _configuration.Debug;
        if (ImGui.Checkbox("Debug", ref debug))
        {
            _configuration.Debug = debug;
            _configuration.Save();
        }

        var frameDebug = _configuration.FrameDebug;
        if (ImGui.Checkbox("FrameDebug", ref frameDebug))
        {
            _configuration.FrameDebug = frameDebug;
            _configuration.Save();
        }

        // Show debug item information when debug mode is enabled
        if (_configuration.Debug && _plugin.ModuleManager?.PvPCombat != null)
        {
            ImGui.Separator();
            ImGui.Text("Debug Item Information:");

            var debugItems = _plugin.ModuleManager.PvPCombat.DebugItems;

            if (debugItems.Any())
            {
                ImGui.Text("Total Items: " + debugItems.Count);

                // Filter options
                bool showOnlyWoodDummies = _configuration.ShowOnlyWoodDummies;
                if (ImGui.Checkbox("Show only 木人 (Training Dummies)", ref showOnlyWoodDummies))
                {
                    _configuration.ShowOnlyWoodDummies = showOnlyWoodDummies;
                    _configuration.Save();
                }

                bool showOnlyAttackable = _configuration.ShowOnlyAttackable;

                if (ImGui.Checkbox("Show only attackable items", ref showOnlyAttackable))
                {
                    _configuration.ShowOnlyAttackable = showOnlyAttackable;
                    _configuration.Save();
                }

                // Sort by distance
                var sortedItems = debugItems
                    .Where(item => !showOnlyWoodDummies || item.IsWoodDummy)
                    .Where(item => !showOnlyAttackable || item.CanAttack)
                    .OrderBy(item => item.Distance)
                    .ToList();

                if (ImGui.BeginChild("DebugItemList", new Vector2(0, 300), true))
                {
                    // Table headers
                    ImGui.Columns(6, "DebugItems", true);
                    ImGui.Text("Name");
                    ImGui.NextColumn();
                    ImGui.Text("Distance");
                    ImGui.NextColumn();
                    ImGui.Text("Can Attack");
                    ImGui.NextColumn();
                    ImGui.Text("Object Kind");
                    ImGui.NextColumn();
                    ImGui.Text("Object ID");
                    ImGui.NextColumn();
                    ImGui.Text("Position");
                    ImGui.NextColumn();
                    ImGui.Separator();

                    foreach (var item in sortedItems.Take(50)) // Limit to 50 items to prevent UI lag
                    {
                        // Color code based on properties
                        Vector4 textColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f); // White default
                        if (item.IsWoodDummy)
                            textColor = new Vector4(0.0f, 1.0f, 0.0f, 1.0f); // Green for wood dummies
                        else if (item.CanAttack)
                            textColor = new Vector4(1.0f, 0.5f, 0.0f, 1.0f); // Orange for attackable

                        ImGui.PushStyleColor(ImGuiCol.Text, textColor);

                        // Name
                        ImGui.Text(string.IsNullOrEmpty(item.Name) ? "[No Name]" : item.Name);
                        ImGui.NextColumn();

                        // Distance
                        ImGui.Text(item.Distance.ToString("F1") + "m");
                        ImGui.NextColumn();

                        // Can Attack
                        ImGui.Text(item.CanAttack ? "Yes" : "No");
                        ImGui.NextColumn();

                        // Object Kind
                        ImGui.Text(item.ObjectKind);
                        ImGui.NextColumn();

                        // Object ID
                        ImGui.Text(item.ObjectId.ToString("X8"));
                        ImGui.NextColumn();

                        // Position
                        ImGui.Text("(" + item.Position.X.ToString("F1") + ", " + item.Position.Y.ToString("F1") + ", " +
                                   item.Position.Z.ToString("F1") + ")");
                        ImGui.NextColumn();

                        ImGui.PopStyleColor();
                    }

                    if (sortedItems.Count > 50)
                    {
                        ImGui.Text("... and " + (sortedItems.Count - 50) + " more items");
                        ImGui.NextColumn();
                        ImGui.NextColumn();
                        ImGui.NextColumn();
                        ImGui.NextColumn();
                        ImGui.NextColumn();
                        ImGui.NextColumn();
                    }
                }

                ImGui.EndChild();

                // Legend
                ImGui.Separator();
                ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), "Green: 木人 (Training Dummies)");
                ImGui.TextColored(new Vector4(1.0f, 0.5f, 0.0f, 1.0f), "Orange: Other Attackable Items");
                ImGui.TextColored(new Vector4(1.0f, 1.0f, 1.0f, 1.0f), "White: Non-attackable Items");
            }
            else
            {
                ImGui.Text("No items found. Make sure you're in-game and debug mode is active.");
            }
        }
    }
}

