using System.Net;
using System.Text;
using System.Text.Json;

namespace TUnit.Engine.Reporters.Html;

internal static class HtmlReportGenerator
{
    internal static string GenerateHtml(ReportData data)
    {
        var sb = new StringBuilder(96 * 1024);
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\" data-theme=\"dark\">");

        AppendHead(sb, data);
        AppendBody(sb, data);

        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static void AppendHead(StringBuilder sb, ReportData data)
    {
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.Append("<title>Test Report \u2014 ");
        sb.Append(WebUtility.HtmlEncode(data.AssemblyName));
        sb.AppendLine("</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(GetCss());
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
    }

    private static void AppendBody(StringBuilder sb, ReportData data)
    {
        sb.AppendLine("<body>");

        // Skip-to-content link for keyboard/screen-reader users
        sb.AppendLine("<a href=\"#testGroups\" class=\"skip-link\">Skip to test results</a>");

        // Ambient background grain
        sb.AppendLine("<div class=\"grain\" aria-hidden=\"true\"></div>");

        // Feature 8: Sticky mini-header
        sb.Append("<div class=\"sticky-bar\" id=\"stickyBar\" aria-hidden=\"true\">");
        sb.Append("<span class=\"sticky-name\">");
        sb.Append(WebUtility.HtmlEncode(data.AssemblyName));
        sb.Append("</span>");
        sb.Append("<span class=\"sticky-badges\">");
        sb.Append("<span class=\"sticky-b sb-pass\">");
        sb.Append(data.Summary.Passed);
        sb.Append("</span>");
        sb.Append("<span class=\"sticky-b sb-fail\">");
        sb.Append(data.Summary.Failed + data.Summary.TimedOut);
        sb.Append("</span>");
        sb.Append("<span class=\"sticky-b sb-skip\">");
        sb.Append(data.Summary.Skipped);
        sb.Append("</span>");
        sb.Append("</span>");
        sb.AppendLine("<button class=\"sticky-search-btn\" id=\"stickySearchBtn\" title=\"Focus search\"><svg viewBox=\"0 0 20 20\" fill=\"currentColor\" width=\"14\" height=\"14\"><path fill-rule=\"evenodd\" d=\"M9 3.5a5.5 5.5 0 1 0 0 11 5.5 5.5 0 0 0 0-11ZM2 9a7 7 0 1 1 12.45 4.39l3.58 3.58a.75.75 0 1 1-1.06 1.06l-3.58-3.58A7 7 0 0 1 2 9Z\" clip-rule=\"evenodd\"/></svg></button>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"shell\">");

        AppendHeader(sb, data);

        sb.AppendLine("<main id=\"main\">");
        AppendSummaryDashboard(sb, data.Summary, data.TotalDurationMs);
        AppendSearchAndFilters(sb, data.Summary);

        // Quick-access sections populated by JS
        sb.AppendLine("<div id=\"failedSection\" role=\"region\" aria-label=\"Failed tests\"></div>");
        sb.AppendLine("<div id=\"slowestSection\" role=\"region\" aria-label=\"Slowest tests\"></div>");

        AppendTestGroups(sb, data);
        sb.AppendLine("</main>");

        AppendJsonData(sb, data);
        AppendJavaScript(sb);

        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
    }

    private static void AppendHeader(StringBuilder sb, ReportData data)
    {
        sb.AppendLine("<header class=\"hdr\">");
        sb.AppendLine("<div class=\"hdr-brand\">");
        // TUnit logo mark
        sb.AppendLine("<svg class=\"hdr-logo\" viewBox=\"0 0 32 32\" fill=\"none\" xmlns=\"http://www.w3.org/2000/svg\">");
        sb.AppendLine("<rect width=\"32\" height=\"32\" rx=\"8\" fill=\"url(#lg)\"/>");
        sb.AppendLine("<path d=\"M8 11h16v3H18.5v10h-5V14H8v-3z\" fill=\"#fff\"/>");
        sb.AppendLine("<defs><linearGradient id=\"lg\" x1=\"0\" y1=\"0\" x2=\"32\" y2=\"32\"><stop stop-color=\"#6366f1\"/><stop offset=\"1\" stop-color=\"#a78bfa\"/></linearGradient></defs>");
        sb.AppendLine("</svg>");
        sb.AppendLine("<div class=\"hdr-titles\">");
        sb.Append("<h1 class=\"hdr-name\">");
        sb.Append(WebUtility.HtmlEncode(data.AssemblyName));
        sb.AppendLine("</h1>");
        sb.AppendLine("<span class=\"hdr-sub\">Test Report</span>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"hdr-meta\">");
        AppendMetaChip(sb, "clock", data.Timestamp);
        AppendMetaChip(sb, "cpu", data.MachineName);
        AppendMetaChip(sb, "os", data.OperatingSystem);
        AppendMetaChip(sb, "runtime", data.RuntimeVersion);
        AppendMetaChip(sb, "tag", "TUnit " + data.TUnitVersion);
        if (!string.IsNullOrEmpty(data.Filter))
        {
            AppendMetaChip(sb, "filter", data.Filter!);
        }

        if (!string.IsNullOrEmpty(data.Branch))
        {
            AppendMetaChip(sb, "branch", data.Branch!);
        }

        if (!string.IsNullOrEmpty(data.CommitSha))
        {
            var shortSha = data.CommitSha!.Length > 7 ? data.CommitSha[..7] : data.CommitSha;
            if (!string.IsNullOrEmpty(data.RepositorySlug))
            {
                AppendMetaChipLink(sb, "commit", shortSha, $"https://github.com/{data.RepositorySlug}/commit/{data.CommitSha}");
            }
            else
            {
                AppendMetaChip(sb, "commit", shortSha);
            }
        }

        if (!string.IsNullOrEmpty(data.PullRequestNumber))
        {
            if (!string.IsNullOrEmpty(data.RepositorySlug))
            {
                AppendMetaChipLink(sb, "pr", $"PR #{data.PullRequestNumber}", $"https://github.com/{data.RepositorySlug}/pull/{data.PullRequestNumber}");
            }
            else
            {
                AppendMetaChip(sb, "pr", $"PR #{data.PullRequestNumber}");
            }
        }

        sb.AppendLine("</div>");

        // Theme toggle button
        sb.AppendLine("<button id=\"themeToggle\" class=\"theme-btn\" aria-label=\"Toggle theme\">");
        sb.AppendLine("<svg class=\"theme-icon theme-sun\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><circle cx=\"12\" cy=\"12\" r=\"4\"/><path d=\"M12 2v2m0 16v2M4.93 4.93l1.41 1.41m11.32 11.32l1.41 1.41M2 12h2m16 0h2M4.93 19.07l1.41-1.41m11.32-11.32l1.41-1.41\"/></svg>");
        sb.AppendLine("<svg class=\"theme-icon theme-moon\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z\"/></svg>");
        sb.AppendLine("</button>");

        sb.AppendLine("</header>");
    }

    private static void AppendMetaChip(StringBuilder sb, string icon, string text)
    {
        sb.Append("<span class=\"chip\"><span class=\"chip-icon\" data-icon=\"");
        sb.Append(icon);
        sb.Append("\"></span>");
        sb.Append(WebUtility.HtmlEncode(text));
        sb.AppendLine("</span>");
    }

    private static void AppendMetaChipLink(StringBuilder sb, string icon, string text, string href)
    {
        sb.Append("<a class=\"chip chip-link\" href=\"");
        sb.Append(WebUtility.HtmlEncode(href));
        sb.Append("\" target=\"_blank\" rel=\"noopener\"><span class=\"chip-icon\" data-icon=\"");
        sb.Append(icon);
        sb.Append("\"></span>");
        sb.Append(WebUtility.HtmlEncode(text));
        sb.AppendLine("</a>");
    }

    private static void AppendSummaryDashboard(StringBuilder sb, ReportSummary summary, double totalDurationMs)
    {
        var passRate = summary.Total > 0 ? (double)summary.Passed / summary.Total * 100 : 0;

        sb.AppendLine("<section class=\"dash\" data-anim=\"fade-up\" aria-label=\"Test summary\">");

        // Ring chart — SVG
        var circumference = 2 * Math.PI * 54; // r=54
        var passLen = summary.Total > 0 ? circumference * summary.Passed / summary.Total : 0;
        var failLen = summary.Total > 0 ? circumference * (summary.Failed + summary.TimedOut) / summary.Total : 0;
        var skipLen = summary.Total > 0 ? circumference * summary.Skipped / summary.Total : 0;
        var cancelLen = summary.Total > 0 ? circumference * summary.Cancelled / summary.Total : 0;

        sb.AppendLine("<div class=\"ring-wrap\">");
        sb.AppendLine("<svg class=\"ring\" viewBox=\"0 0 120 120\">");
        // Track
        sb.AppendLine("<circle cx=\"60\" cy=\"60\" r=\"54\" fill=\"none\" stroke=\"var(--surface-2)\" stroke-width=\"10\"/>");

        // Segments — stacked with dasharray/dashoffset
        double offset = 0;
        if (passLen > 0)
        {
            AppendRingSegment(sb, "var(--emerald)", passLen, offset, circumference);
            offset += passLen;
        }

        if (failLen > 0)
        {
            AppendRingSegment(sb, "var(--rose)", failLen, offset, circumference);
            offset += failLen;
        }

        if (skipLen > 0)
        {
            AppendRingSegment(sb, "var(--amber)", skipLen, offset, circumference);
            offset += skipLen;
        }

        if (cancelLen > 0)
        {
            AppendRingSegment(sb, "var(--slate)", cancelLen, offset, circumference);
        }

        sb.AppendLine("</svg>");
        sb.Append("<div class=\"ring-center\"><span class=\"ring-pct\">");
        sb.Append(passRate.ToString("F0"));
        sb.Append("<small>%</small></span><span class=\"ring-lbl\">");
        sb.Append(summary.Total > 0 ? "pass rate" : "no tests");
        sb.AppendLine("</span></div>");
        sb.AppendLine("</div>");

        // Stat cards
        sb.AppendLine("<div class=\"stats\">");
        AppendStatCard(sb, "total", summary.Total.ToString(), "Total", null);
        AppendStatCard(sb, "passed", summary.Passed.ToString(), "Passed", "var(--emerald)");
        AppendStatCard(sb, "failed", (summary.Failed + summary.TimedOut).ToString(), "Failed", "var(--rose)");
        AppendStatCard(sb, "skipped", summary.Skipped.ToString(), "Skipped", "var(--amber)");
        AppendStatCard(sb, "cancelled", summary.Cancelled.ToString(), "Cancelled", "var(--slate)");
        sb.AppendLine("</div>");

        // Duration
        sb.AppendLine("<div class=\"dash-dur\">");
        sb.Append("<span class=\"dash-dur-val\">");
        sb.Append(FormatDuration(totalDurationMs));
        sb.AppendLine("</span>");
        sb.AppendLine("<span class=\"dash-dur-lbl\">duration</span>");
        sb.AppendLine("<div id=\"durationHist\" class=\"dur-hist\"></div>");
        sb.AppendLine("</div>");

        sb.AppendLine("</section>");
    }

    private static void AppendRingSegment(StringBuilder sb, string color, double len, double offset, double circumference)
    {
        sb.Append("<circle class=\"ring-seg\" cx=\"60\" cy=\"60\" r=\"54\" fill=\"none\" stroke=\"");
        sb.Append(color);
        sb.Append("\" stroke-width=\"10\" stroke-linecap=\"round\" stroke-dasharray=\"");
        sb.Append(len.ToString("F2"));
        sb.Append(' ');
        sb.Append((circumference - len).ToString("F2"));
        sb.Append("\" stroke-dashoffset=\"-");
        sb.Append(offset.ToString("F2"));
        sb.AppendLine("\" transform=\"rotate(-90 60 60)\"/>");
    }

    private static void AppendStatCard(StringBuilder sb, string cls, string count, string label, string? accent)
    {
        sb.Append("<div class=\"stat ");
        sb.Append(cls);
        sb.Append("\"");
        if (accent != null)
        {
            sb.Append(" style=\"--accent:");
            sb.Append(accent);
            sb.Append("\"");
        }

        sb.AppendLine(">");
        sb.Append("<span class=\"stat-n\">");
        sb.Append(count);
        sb.Append("</span><span class=\"stat-l\">");
        sb.Append(label);
        sb.AppendLine("</span></div>");
    }

    private static void AppendSearchAndFilters(StringBuilder sb, ReportSummary summary)
    {
        sb.AppendLine("<div class=\"bar\" data-anim=\"fade-up\">");
        sb.AppendLine("<div class=\"search\" role=\"search\">");
        // Search icon inline SVG
        sb.AppendLine("<svg class=\"search-icon\" viewBox=\"0 0 20 20\" fill=\"currentColor\" aria-hidden=\"true\"><path fill-rule=\"evenodd\" d=\"M9 3.5a5.5 5.5 0 1 0 0 11 5.5 5.5 0 0 0 0-11ZM2 9a7 7 0 1 1 12.45 4.39l3.58 3.58a.75.75 0 1 1-1.06 1.06l-3.58-3.58A7 7 0 0 1 2 9Z\" clip-rule=\"evenodd\"/></svg>");
        sb.AppendLine("<input type=\"text\" id=\"searchInput\" placeholder=\"Search tests\u2026\" autocomplete=\"off\" spellcheck=\"false\" aria-label=\"Search tests\">");
        sb.AppendLine("<button id=\"clearSearch\" class=\"search-clear\" aria-label=\"Clear search\">&times;</button>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"pills\" id=\"filterButtons\" role=\"group\" aria-label=\"Filter by status\">");
        sb.Append("<button class=\"pill active\" data-filter=\"all\" aria-pressed=\"true\">All <span class=\"pill-count\">");
        sb.Append(summary.Total);
        sb.AppendLine("</span></button>");
        sb.Append("<button class=\"pill\" data-filter=\"passed\" aria-pressed=\"false\"><span class=\"dot emerald\"></span>Passed <span class=\"pill-count\">");
        sb.Append(summary.Passed);
        sb.AppendLine("</span></button>");
        sb.Append("<button class=\"pill\" data-filter=\"failed\" aria-pressed=\"false\"><span class=\"dot rose\"></span>Failed <span class=\"pill-count\">");
        sb.Append(summary.Failed + summary.TimedOut);
        sb.AppendLine("</span></button>");
        sb.Append("<button class=\"pill\" data-filter=\"skipped\" aria-pressed=\"false\"><span class=\"dot amber\"></span>Skipped <span class=\"pill-count\">");
        sb.Append(summary.Skipped);
        sb.AppendLine("</span></button>");
        sb.Append("<button class=\"pill\" data-filter=\"cancelled\" aria-pressed=\"false\"><span class=\"dot slate\"></span>Cancelled <span class=\"pill-count\">");
        sb.Append(summary.Cancelled);
        sb.AppendLine("</span></button>");
        sb.AppendLine("</div>");

        // Feature 2: Expand/Collapse All + Feature 3: Sort Toggle
        sb.AppendLine("<div class=\"bar-actions\">");
        sb.AppendLine("<button id=\"expandAll\" class=\"bar-btn\" aria-label=\"Expand all groups\" title=\"Expand all groups\"><svg viewBox=\"0 0 16 16\" fill=\"currentColor\" width=\"14\" height=\"14\" aria-hidden=\"true\"><path d=\"M1.75 10a.75.75 0 0 1 .75.75v2.5h2.5a.75.75 0 0 1 0 1.5h-3.25a.75.75 0 0 1-.75-.75v-3.25a.75.75 0 0 1 .75-.75Zm12.5 0a.75.75 0 0 1 .75.75v3.25a.75.75 0 0 1-.75.75h-3.25a.75.75 0 0 1 0-1.5h2.5v-2.5a.75.75 0 0 1 .75-.75ZM2.5 2.25h-2.5v2.5a.75.75 0 0 1-1.5 0v-3.25a.75.75 0 0 1 .75-.75h3.25a.75.75 0 0 1 0 1.5Zm8.75-1.5a.75.75 0 0 1 0-1.5h3.25a.75.75 0 0 1 .75.75v3.25a.75.75 0 0 1-1.5 0v-2.5h-2.5Z\"/></svg></button>");
        sb.AppendLine("<button id=\"collapseAll\" class=\"bar-btn\" aria-label=\"Collapse all groups\" title=\"Collapse all groups\"><svg viewBox=\"0 0 16 16\" fill=\"currentColor\" width=\"14\" height=\"14\" aria-hidden=\"true\"><path d=\"M3.75 14a.75.75 0 0 1-.75-.75v-2.5h-2.5a.75.75 0 0 1 0-1.5h3.25a.75.75 0 0 1 .75.75v3.25a.75.75 0 0 1-.75.75Zm8.5 0a.75.75 0 0 1-.75-.75v-3.25a.75.75 0 0 1 .75-.75h3.25a.75.75 0 0 1 0 1.5h-2.5v2.5a.75.75 0 0 1-.75.75ZM.5 4.75a.75.75 0 0 1 0-1.5h2.5v-2.5a.75.75 0 0 1 1.5 0v3.25a.75.75 0 0 1-.75.75H.5Zm11 0a.75.75 0 0 1-.75-.75v-3.25a.75.75 0 0 1 1.5 0v2.5h2.5a.75.75 0 0 1 0 1.5h-3.25Z\"/></svg></button>");
        sb.AppendLine("<span class=\"bar-sep\"></span>");
        sb.AppendLine("<span class=\"bar-lbl\" id=\"groupLabel\">Group:</span>");
        sb.AppendLine("<div class=\"grp-toggle\" role=\"radiogroup\" aria-labelledby=\"groupLabel\">");
        sb.AppendLine("<button class=\"sort-btn active\" data-group=\"class\" role=\"radio\" aria-checked=\"true\">Class</button>");
        sb.AppendLine("<button class=\"sort-btn\" data-group=\"namespace\" role=\"radio\" aria-checked=\"false\">Namespace</button>");
        sb.AppendLine("<button class=\"sort-btn\" data-group=\"status\" role=\"radio\" aria-checked=\"false\">Status</button>");
        sb.AppendLine("</div>");
        sb.AppendLine("<span class=\"bar-sep\"></span>");
        sb.AppendLine("<span class=\"bar-lbl\" id=\"sortLabel\">Sort:</span>");
        sb.AppendLine("<div class=\"sort-group\" role=\"radiogroup\" aria-labelledby=\"sortLabel\">");
        sb.AppendLine("<button class=\"sort-btn active\" data-sort=\"default\" role=\"radio\" aria-checked=\"true\" title=\"Failures first\">Default</button>");
        sb.AppendLine("<button class=\"sort-btn\" data-sort=\"duration\" role=\"radio\" aria-checked=\"false\" title=\"Slowest first\">Duration</button>");
        sb.AppendLine("<button class=\"sort-btn\" data-sort=\"name\" role=\"radio\" aria-checked=\"false\" title=\"Alphabetical\">Name</button>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<span class=\"bar-info\" id=\"filterSummary\" aria-live=\"polite\" aria-atomic=\"true\"></span>");
        sb.AppendLine("</div>");
    }

    private static void AppendTestGroups(StringBuilder sb, ReportData data)
    {
        if (data.Summary.Total == 0)
        {
            sb.AppendLine("<div class=\"empty\">No tests were discovered.</div>");
            return;
        }

        sb.AppendLine("<div id=\"globalTimeline\"></div>");
        sb.AppendLine("<div id=\"testGroups\" class=\"groups\"></div>");
    }

    private static void AppendJsonData(StringBuilder sb, ReportData data)
    {
        sb.Append("<script id=\"test-data\" type=\"application/json\">");
        var json = JsonSerializer.Serialize(data, HtmlReportJsonContext.Default.ReportData);
        sb.Append(json.Replace("</", "<\\/"));
        sb.AppendLine("</script>");
    }

    private static void AppendJavaScript(StringBuilder sb)
    {
        sb.AppendLine("<script>");
        sb.AppendLine(GetJavaScript());
        sb.AppendLine("</script>");
    }

    private static string FormatDuration(double ms)
    {
        if (ms < 1)
        {
            return "<1ms";
        }

        // Show milliseconds for anything under 1 second (avoids rounding 999ms to "1.00s")
        if (Math.Round(ms) < 1000)
        {
            return $"{ms:F0}ms";
        }

        if (ms < 60000)
        {
            return $"{ms / 1000:F2}s";
        }

        return $"{ms / 60000:F1}m";
    }

    private static string GetCss()
    {
        return """
/* ═══════════════════════════════════════════════════════
   TUnit — Dark Observatory Report Theme
   ═══════════════════════════════════════════════════════ */

/* ── Design Tokens ─────────────────────────────────── */
:root {
  --bg:        #0b0d11;
  --surface-0: #12151c;
  --surface-1: #181c25;
  --surface-2: #1f2430;
  --surface-3: #282e3a;
  --border:    rgba(255,255,255,.06);
  --border-h:  rgba(255,255,255,.10);

  --text:      #e2e4e9;
  --text-2:    #9ba1b0;
  --text-3:    #5f6678;

  --emerald:   #34d399;
  --emerald-d: rgba(52,211,153,.12);
  --rose:      #fb7185;
  --rose-d:    rgba(251,113,133,.12);
  --amber:     #fbbf24;
  --amber-d:   rgba(251,191,36,.10);
  --slate:     #94a3b8;
  --slate-d:   rgba(148,163,184,.10);
  --indigo:    #818cf8;
  --indigo-d:  rgba(129,140,248,.10);

  --font:      'Segoe UI Variable','Segoe UI',-apple-system,BlinkMacSystemFont,system-ui,sans-serif;
  --mono:      'Cascadia Code','JetBrains Mono','Fira Code','SF Mono',ui-monospace,monospace;

  --r:         8px;
  --r-lg:      14px;
  --ease:      cubic-bezier(.4,0,.2,1);
}

/* ── Light Theme ──────────────────────────────────── */
:root[data-theme="light"]{
  --bg:#f8f9fb;--surface-0:#ffffff;--surface-1:#f0f1f4;--surface-2:#e4e6eb;--surface-3:#d1d5de;
  --border:rgba(0,0,0,.08);--border-h:rgba(0,0,0,.14);
  --text:#1a1d24;--text-2:#5a5f6e;--text-3:#8b91a0;
  --emerald-d:rgba(52,211,153,.15);--rose-d:rgba(251,113,133,.15);
  --amber-d:rgba(251,191,36,.12);--slate-d:rgba(148,163,184,.12);
  --indigo-d:rgba(129,140,248,.12);
}
:root[data-theme="light"] .grain{opacity:.008}

/* ── Theme Transition ─────────────────────────────── */
.theme-transition,.theme-transition *,.theme-transition *::before,.theme-transition *::after{
  transition:background-color .3s var(--ease),color .3s var(--ease),border-color .3s var(--ease),box-shadow .3s var(--ease)!important;
}

/* ── Reset & Base ──────────────────────────────────── */
*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}
html{-webkit-font-smoothing:antialiased;-moz-osx-font-smoothing:grayscale}
body{
  font-family:var(--font);background:var(--bg);color:var(--text);
  line-height:1.55;font-size:14px;min-height:100vh;
  overflow-x:hidden;
}

/* Film-grain overlay for atmosphere */
.grain{
  position:fixed;inset:0;pointer-events:none;z-index:9999;opacity:.018;
  background:url("data:image/svg+xml,%3Csvg viewBox='0 0 256 256' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='.85' numOctaves='4' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)'/%3E%3C/svg%3E");
}

/* ── Shell ─────────────────────────────────────────── */
.shell{max-width:1360px;margin:0 auto;padding:28px 24px 64px}

/* ── Animations ────────────────────────────────────── */
@keyframes fade-up{
  from{opacity:0;transform:translateY(14px)}
  to{opacity:1;transform:none}
}
@keyframes ring-draw{
  from{stroke-dashoffset:340}
}
[data-anim]{animation:fade-up .5s var(--ease) both}
[data-anim]:nth-child(2){animation-delay:.08s}
[data-anim]:nth-child(3){animation-delay:.16s}
.ring-seg{animation:ring-draw .9s var(--ease) both}

/* Stat card stagger */
[data-anim] .stat{animation:fade-up .4s var(--ease) both}
[data-anim] .stat:nth-child(1){animation-delay:0s}
[data-anim] .stat:nth-child(2){animation-delay:.05s}
[data-anim] .stat:nth-child(3){animation-delay:.1s}
[data-anim] .stat:nth-child(4){animation-delay:.15s}
[data-anim] .stat:nth-child(5){animation-delay:.2s}

/* Test row stagger on group open */
.grp.open .t-row{animation:fade-up .3s var(--ease) both;animation-delay:calc(var(--row-idx,0) * .03s)}

/* Reduced motion */
@media(prefers-reduced-motion:reduce){*,*::before,*::after{animation-duration:.01ms!important;animation-iteration-count:1!important;transition-duration:.01ms!important}}

/* ── Header ────────────────────────────────────────── */
.hdr{
  display:flex;align-items:center;flex-wrap:wrap;
  gap:10px 14px;margin-bottom:16px;
  animation:fade-up .5s var(--ease) both;
}
.hdr-brand{display:flex;align-items:center;gap:12px}
.hdr-logo{width:38px;height:38px;flex-shrink:0}
.hdr-name{
  font-size:1.35rem;font-weight:700;letter-spacing:-.02em;
  background:linear-gradient(135deg,#e2e4e9 30%,#818cf8);
  -webkit-background-clip:text;-webkit-text-fill-color:transparent;
  background-clip:text;
}
:root[data-theme="light"] .hdr-name{background:linear-gradient(135deg,#1a1d24 30%,#6366f1);-webkit-background-clip:text;background-clip:text}
.hdr-sub{font-size:.78rem;color:var(--text-3);letter-spacing:.06em;text-transform:uppercase}
.hdr-meta{display:flex;gap:6px;flex-wrap:wrap;align-items:center;margin-left:auto}
.chip{
  display:inline-flex;align-items:center;gap:6px;
  padding:5px 12px;border-radius:100px;
  background:var(--surface-1);border:1px solid var(--border);
  font-size:.78rem;color:var(--text-2);white-space:nowrap;
}
.chip-link{
  text-decoration:none;cursor:pointer;
  transition:border-color .2s var(--ease),background .2s var(--ease);
}
.chip-link:hover{border-color:var(--indigo);background:var(--indigo-d);color:var(--text)}

/* ── Theme Toggle ─────────────────────────────────── */
.theme-btn{
  display:flex;align-items:center;justify-content:center;
  width:36px;height:36px;border-radius:100px;
  background:var(--surface-1);border:1px solid var(--border);
  color:var(--text-2);cursor:pointer;flex-shrink:0;
  transition:border-color .2s var(--ease),color .2s var(--ease),background .2s var(--ease);
}
.theme-btn:hover{border-color:var(--border-h);color:var(--text)}
.theme-btn svg{width:18px;height:18px;transition:transform .3s var(--ease)}
.theme-btn:active svg{transform:rotate(30deg)}
[data-theme="dark"] .theme-sun{display:none}
[data-theme="light"] .theme-moon{display:none}

/* ── Dashboard ─────────────────────────────────────── */
.dash{
  display:flex;align-items:center;gap:32px;flex-wrap:wrap;
  padding:28px;margin-bottom:24px;
  background:var(--surface-0);
  border:1px solid var(--border);border-radius:var(--r-lg);
  position:relative;overflow:hidden;
}
.dash::before{
  content:'';position:absolute;inset:0;
  background:radial-gradient(ellipse 60% 50% at 20% 50%,rgba(99,102,241,.04),transparent),
             radial-gradient(ellipse 40% 60% at 80% 30%,rgba(52,211,153,.03),transparent);
  pointer-events:none;
}

/* Ring */
.ring-wrap{position:relative;width:130px;height:130px;flex-shrink:0}
.ring{width:100%;height:100%}
.ring-center{
  position:absolute;inset:0;display:flex;flex-direction:column;
  align-items:center;justify-content:center;
}
.ring-pct{font-size:1.7rem;font-weight:800;letter-spacing:-.03em;line-height:1}
.ring-pct small{font-size:.55em;font-weight:600;opacity:.6}
.ring-lbl{font-size:.68rem;color:var(--text-3);margin-top:2px;letter-spacing:.04em;text-transform:uppercase}

/* Stat cards */
.stats{display:flex;gap:10px;flex-wrap:wrap;flex:1}
.stat{
  position:relative;flex:1;min-width:88px;
  padding:16px 14px;border-radius:var(--r);
  background:var(--surface-1);border:1px solid var(--border);
  text-align:center;transition:border-color .2s var(--ease),transform .2s var(--ease);
  overflow:hidden;
}
.stat::after{
  content:'';position:absolute;top:0;left:0;right:0;height:2px;
  background:var(--accent,var(--indigo));border-radius:2px 2px 0 0;
  opacity:.7;
}
.stat:hover{border-color:var(--border-h);transform:translateY(-1px)}
.stat-n{display:block;font-size:1.65rem;font-weight:800;letter-spacing:-.03em;line-height:1.1;font-variant-numeric:tabular-nums}
.stat-l{display:block;font-size:.72rem;color:var(--text-3);margin-top:4px;text-transform:uppercase;letter-spacing:.06em}

/* coloured numbers */
.stat.passed  .stat-n{color:var(--emerald)}
.stat.failed  .stat-n{color:var(--rose)}
.stat.skipped .stat-n{color:var(--amber)}

/* Duration */
.dash-dur{text-align:center;padding:4px 20px;flex-shrink:0}
.dash-dur-val{display:block;font-size:1.5rem;font-weight:800;font-family:var(--mono);letter-spacing:-.02em}
.dash-dur-lbl{display:block;font-size:.68rem;color:var(--text-3);text-transform:uppercase;letter-spacing:.06em;margin-top:2px}

/* ── Toolbar (search + pills) ──────────────────────── */
.bar{display:flex;align-items:center;gap:12px;flex-wrap:wrap;margin-bottom:16px;justify-content:flex-end}
.search{
  position:relative;flex:1;min-width:220px;
}
.search-icon{
  position:absolute;left:11px;top:50%;transform:translateY(-50%);
  width:16px;height:16px;color:var(--text-3);pointer-events:none;
}
.search input{
  width:100%;padding:9px 34px 9px 34px;
  background:var(--surface-1);border:1px solid var(--border);border-radius:var(--r);
  color:var(--text);font-size:.88rem;font-family:var(--font);
  transition:border-color .2s var(--ease),box-shadow .2s var(--ease);
  outline:none;
}
.search input::placeholder{color:var(--text-3)}
.search input:focus{border-color:rgba(129,140,248,.4);box-shadow:0 0 0 3px rgba(129,140,248,.08)}
.search-clear{
  position:absolute;right:8px;top:50%;transform:translateY(-50%);
  background:none;border:none;color:var(--text-3);font-size:1.15rem;
  cursor:pointer;display:none;line-height:1;padding:2px;
}
.search-clear:hover{color:var(--text)}

/* Filter pills */
.pills{display:flex;gap:5px}
.pill{
  display:inline-flex;align-items:center;gap:5px;
  padding:7px 14px;border-radius:100px;
  background:var(--surface-1);border:1px solid var(--border);
  color:var(--text-2);font-size:.8rem;cursor:pointer;
  font-family:var(--font);
  transition:all .18s var(--ease);white-space:nowrap;
}
.pill:hover{border-color:var(--border-h);color:var(--text)}
.pill.active{background:var(--indigo);border-color:var(--indigo);color:#fff}
.dot{width:7px;height:7px;border-radius:50%;display:inline-block}
.dot.emerald{background:var(--emerald)}
.dot.rose{background:var(--rose)}
.dot.amber{background:var(--amber)}
.dot.slate{background:var(--slate)}
.bar-info{font-size:.8rem;color:var(--text-3);margin-left:auto}

/* ── Groups ────────────────────────────────────────── */
.groups{display:flex;flex-direction:column;gap:6px}
.grp{
  background:var(--surface-0);border:1px solid var(--border);
  border-radius:var(--r);overflow:hidden;
  transition:border-color .2s var(--ease);
}
.grp:hover{border-color:var(--border-h)}
.grp-hd{
  display:flex;align-items:center;gap:12px;padding:9px 16px;
  cursor:pointer;user-select:none;
  transition:background .15s var(--ease);
}
.grp-hd:hover{background:var(--surface-1)}
.grp-arrow{
  width:16px;height:16px;color:var(--text-3);flex-shrink:0;
  transition:transform .2s var(--ease);
}
.grp.open .grp-arrow{transform:rotate(90deg)}
.grp-name{font-weight:600;font-size:.9rem;flex:1;word-break:break-word}
.grp-badges{display:flex;gap:6px;flex-shrink:0}
.grp-b{
  display:inline-flex;align-items:center;gap:3px;
  font-size:.72rem;padding:2px 8px;border-radius:100px;
  font-weight:600;font-variant-numeric:tabular-nums;
}
.grp-b.gp{background:var(--emerald-d);color:var(--emerald)}
.grp-b.gf{background:var(--rose-d);color:var(--rose)}
.grp-b.gs{background:var(--amber-d);color:var(--amber)}
.grp-b.gt{color:var(--text-3);font-weight:500}
.grp-indicator{
  width:4px;height:18px;border-radius:2px;flex-shrink:0;
  background:var(--emerald);opacity:.6;
}
.grp-hd.fail .grp-indicator{background:var(--rose)}
.grp-body{display:grid;grid-template-rows:0fr;transition:grid-template-rows .3s var(--ease)}
.grp.open .grp-body{grid-template-rows:1fr}
.grp-body-inner{overflow:hidden;min-height:0}
.grp-body-pad{border-top:1px solid var(--border)}

/* ── Test Rows ─────────────────────────────────────── */
.t-row{
  display:flex;align-items:center;gap:10px;
  padding:9px 16px 9px 20px;
  border-bottom:1px solid var(--border);
  cursor:pointer;transition:background .12s var(--ease);
}
.t-row:last-child{border-bottom:none}
.t-row:hover{background:rgba(255,255,255,.02)}
.t-badge{
  font-size:.7rem;font-weight:700;padding:3px 9px;border-radius:100px;
  text-transform:uppercase;letter-spacing:.04em;white-space:nowrap;
  line-height:1;
}
.t-badge.passed{background:var(--emerald-d);color:var(--emerald);box-shadow:0 0 6px rgba(52,211,153,.15)}
.t-badge.failed,.t-badge.error,.t-badge.timedOut{background:var(--rose-d);color:var(--rose);box-shadow:0 0 6px rgba(251,113,133,.15)}
.t-badge.skipped{background:var(--amber-d);color:var(--amber);box-shadow:0 0 6px rgba(251,191,36,.12)}
.t-badge.cancelled{background:var(--slate-d);color:var(--slate)}
.t-badge.inProgress,.t-badge.unknown{background:var(--surface-2);color:var(--text-3)}
.t-name{flex:1;font-size:.88rem;word-break:break-word;color:var(--text)}
.t-dur{font-size:.78rem;color:var(--text-3);font-family:var(--mono);white-space:nowrap;font-variant-numeric:tabular-nums}
.retry-tag{
  font-size:.65rem;font-weight:700;padding:2px 7px;border-radius:4px;
  background:var(--amber-d);color:var(--amber);white-space:nowrap;
}

/* ── Test Detail Panel ─────────────────────────────── */
.t-detail{
  display:grid;grid-template-rows:0fr;
  transition:grid-template-rows .3s var(--ease);
}
.t-detail.open{grid-template-rows:1fr}
.t-detail-inner{overflow:hidden;min-height:0}
.t-detail-pad{padding:14px 18px 14px 22px;background:var(--surface-1);border-bottom:1px solid var(--border)}
.d-sec{margin-bottom:14px}
.d-sec:last-child{margin-bottom:0}
.d-info{
  display:flex;gap:20px;flex-wrap:wrap;
  padding:10px 14px;border-radius:var(--r);
  background:var(--surface-0);border:1px solid var(--border);
}
.d-info-item{font-size:.82rem;color:var(--text-2)}
.d-info-label{font-size:.68rem;font-weight:700;text-transform:uppercase;color:var(--text-3);letter-spacing:.07em;margin-right:4px}
.d-collapsible .d-collapse-toggle{
  display:flex;align-items:center;gap:6px;cursor:pointer;user-select:none;
  font-size:.68rem;font-weight:700;text-transform:uppercase;
  color:var(--text-3);letter-spacing:.07em;margin-bottom:5px;
  transition:color .15s var(--ease);
}
.d-collapsible .d-collapse-toggle:hover{color:var(--text)}
.d-collapsible .d-collapse-toggle .tl-arrow{transition:transform .2s var(--ease);flex-shrink:0}
.d-collapsible.d-col-open .d-collapse-toggle .tl-arrow{transform:rotate(90deg)}
.d-collapsible .d-collapse-content{display:grid;grid-template-rows:0fr;transition:grid-template-rows .3s var(--ease)}
.d-collapsible.d-col-open .d-collapse-content{grid-template-rows:1fr}
.d-collapse-inner{overflow:hidden;min-height:0}
.d-lbl{
  font-size:.68rem;font-weight:700;text-transform:uppercase;
  color:var(--text-3);margin-bottom:5px;letter-spacing:.07em;
}
.d-pre{
  background:var(--surface-0);border:1px solid var(--border);
  border-radius:var(--r);padding:10px 12px;
  font-family:var(--mono);font-size:.8rem;color:var(--text-2);
  white-space:pre-wrap;word-break:break-word;
  max-height:320px;overflow:auto;line-height:1.5;
  border-left:2px solid var(--indigo);
}
.d-pre.err{color:var(--rose);border-color:rgba(251,113,133,.15);border-left:2px solid var(--rose)}
.d-pre.stack{color:var(--text-3);font-size:.76rem;border-left-color:var(--text-3)}
.d-tags{display:flex;gap:6px;flex-wrap:wrap}
.d-tag{
  padding:3px 10px;border-radius:100px;font-size:.76rem;
  background:var(--surface-2);color:var(--text-2);border:1px solid var(--border);
}
.d-tag.kv{font-family:var(--mono);font-size:.72rem}

.d-src{font-size:.78rem;color:var(--text-3);font-family:var(--mono)}

/* ── Copy Button ──────────────────────────────────── */
.d-pre-wrap{position:relative}
.d-pre-wrap .d-pre{margin:0}
.copy-btn{
  position:absolute;top:6px;right:6px;
  background:var(--surface-2);border:1px solid var(--border);border-radius:var(--r);
  color:var(--text-3);cursor:pointer;padding:4px 6px;
  opacity:0;transition:opacity .15s var(--ease),color .15s var(--ease),border-color .15s var(--ease);
  display:flex;align-items:center;justify-content:center;
}
.d-pre-wrap:hover .copy-btn{opacity:1}
.copy-btn:hover{color:var(--text);border-color:var(--border-h)}
.copy-btn.copied{color:var(--emerald);border-color:var(--emerald)}
.copy-btn svg{width:14px;height:14px}

/* ── Trace Timeline ────────────────────────────────── */
.trace{margin-top:6px}
.sp-row{display:flex;align-items:center;gap:6px;padding:2px 0;font-size:.78rem;cursor:pointer}
.sp-row:hover .sp-bar{filter:brightness(1.2)}
.sp-indent{flex-shrink:0}
.sp-bar{height:14px;border-radius:3px;min-width:3px;transition:filter .15s}
.sp-bar.ok{background:linear-gradient(90deg,rgba(52,211,153,.6),var(--emerald))}
.sp-bar.err{background:linear-gradient(90deg,rgba(251,113,133,.6),var(--rose))}
.sp-bar.unk{background:linear-gradient(90deg,rgba(148,163,184,.4),var(--slate))}
.sp-name{color:var(--text-2);max-width:180px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.sp-dur{font-family:var(--mono);color:var(--text-3);font-size:.72rem}
.sp-extra{
  display:none;padding:6px 10px;margin:2px 0 4px;
  background:var(--surface-0);border:1px solid var(--border);border-radius:var(--r);
  font-size:.76rem;color:var(--text-2);
}
.sp-extra.open{display:block;animation:fade-up .2s var(--ease)}
.global-trace,.suite-trace{
  background:var(--surface-1);border:1px solid var(--border);border-radius:var(--r-lg);
  padding:0;margin-bottom:16px;overflow:hidden;
}
.suite-trace{margin:0 0 12px;background:var(--surface-0);border-radius:var(--r)}
.tl-toggle{
  display:flex;align-items:center;gap:6px;padding:12px 16px;cursor:pointer;
  user-select:none;font-size:.82rem;font-weight:600;color:var(--text-2);
  transition:color .15s var(--ease);
}
.suite-trace .tl-toggle{padding:10px 14px;font-size:.78rem}
.tl-toggle:hover{color:var(--text)}
.tl-toggle .tl-arrow{transition:transform .2s var(--ease);flex-shrink:0}
.tl-open .tl-arrow{transform:rotate(90deg)}
.tl-content{display:grid;grid-template-rows:0fr;transition:grid-template-rows .3s var(--ease)}
.tl-open .tl-content{grid-template-rows:1fr}
.tl-content-inner{overflow:hidden;min-height:0}
.tl-content-pad{padding:0 16px 14px}
.suite-trace .tl-content-pad{padding:0 14px 10px}

/* ── Group Summary ─────────────────────────────────── */
.grp-summary{
  display:flex;gap:20px;flex-wrap:wrap;align-items:center;
  padding:10px 16px;background:var(--surface-1);
  border-bottom:1px solid var(--border);
  font-size:.82rem;color:var(--text-2);
}
.grp-summary .d-info-label{font-size:.68rem;font-weight:700;text-transform:uppercase;color:var(--text-3);letter-spacing:.07em;margin-right:4px}
.grp-summary .grp-sum-dur{font-family:var(--mono);font-weight:600;color:var(--text)}

/* ── Quick-Access Sections ─────────────────────────── */
.qa-section{
  background:var(--surface-0);border:1px solid var(--border);border-radius:var(--r-lg);
  margin-bottom:16px;overflow:hidden;
}
.qa-section .tl-toggle{padding:12px 16px;font-size:.82rem;font-weight:600;color:var(--text-2)}
.qa-section .tl-content-pad{padding:0 16px 12px}
.qa-item{
  display:flex;align-items:center;gap:10px;padding:8px 12px;
  border-radius:var(--r);cursor:pointer;
  transition:background .12s var(--ease);
}
.qa-item:hover{background:var(--surface-2)}
.qa-info{flex:1;min-width:0}
.qa-info-name{font-size:.86rem;color:var(--text);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.qa-info-class{font-size:.72rem;color:var(--text-3)}
.qa-err{
  font-size:.74rem;color:var(--rose);font-family:var(--mono);
  white-space:nowrap;overflow:hidden;text-overflow:ellipsis;
  max-width:340px;
}
.qa-dur{font-size:.78rem;color:var(--text-3);font-family:var(--mono);white-space:nowrap;flex-shrink:0;font-variant-numeric:tabular-nums}

/* Slowest tests */
.slow-rank{
  font-size:.72rem;font-weight:800;color:var(--text-3);
  min-width:22px;text-align:center;flex-shrink:0;
  font-variant-numeric:tabular-nums;
}
.slow-bar-track{flex:1;height:6px;border-radius:3px;background:var(--surface-2);overflow:hidden;min-width:60px}
.slow-bar-fill{height:100%;border-radius:3px;background:linear-gradient(90deg,var(--amber),var(--rose));min-width:2px}

/* ── Highlight Flash ───────────────────────────────── */
@keyframes flash-highlight{
  0%{background:rgba(129,140,248,.18)}
  100%{background:transparent}
}
.qa-highlight{animation:flash-highlight 1.5s var(--ease)}

/* ── Empty State ───────────────────────────────────── */
.empty{
  text-align:center;padding:64px 24px;
  color:var(--text-3);font-size:1rem;
  background:var(--surface-0);border:1px solid var(--border);border-radius:var(--r-lg);
}

/* ── Responsive ────────────────────────────────────── */
@media(max-width:768px){
  .shell{padding:16px 12px 48px}
  .dash{flex-direction:column;align-items:stretch;gap:20px;padding:20px}
  .ring-wrap{align-self:center}
  .stats{justify-content:center}
  .bar{flex-direction:column;align-items:stretch}
  .search{max-width:none}
  .hdr{flex-direction:column}
  .qa-err{display:none}
}

/* ── Print ─────────────────────────────────────────── */
@media print{
  :root{--bg:#fff;--surface-0:#fff;--surface-1:#f9f9f9;--surface-2:#f0f0f0;--surface-3:#e0e0e0;
    --border:rgba(0,0,0,.1);--border-h:rgba(0,0,0,.2);--text:#000;--text-2:#333;--text-3:#666}
  body{background:#fff;color:#000}
  .grain,.bar,.theme-btn{display:none}
  .grp-body{grid-template-rows:1fr!important}
  .t-detail{grid-template-rows:1fr!important;background:#f9f9f9}
  .shell{max-width:none;padding:0}
}

/* ── Scrollbar ─────────────────────────────────────── */
::-webkit-scrollbar{width:6px;height:6px}
::-webkit-scrollbar-track{background:transparent}
::-webkit-scrollbar-thumb{background:var(--surface-3);border-radius:3px}
::-webkit-scrollbar-thumb:hover{background:var(--text-3)}

/* ── Light Mode Hover Adjustments ─────────────────── */
:root[data-theme="light"] .t-row:hover{background:rgba(0,0,0,.02)}
:root[data-theme="light"] .dash::before{
  background:radial-gradient(ellipse 60% 50% at 20% 50%,rgba(99,102,241,.06),transparent),
             radial-gradient(ellipse 40% 60% at 80% 30%,rgba(52,211,153,.04),transparent);
}

/* ── Feature 1: Pill Counts ───────────────────────── */
.pill-count{font-size:.72rem;opacity:.7;font-variant-numeric:tabular-nums;margin-left:2px}
.pill.active .pill-count{opacity:.85}

/* ── Feature 2: Expand/Collapse All ──────────────── */
.bar-actions{display:flex;align-items:center;gap:6px;flex-basis:100%;justify-content:flex-end}
.bar-btn{
  display:flex;align-items:center;justify-content:center;
  width:32px;height:32px;border-radius:var(--r);
  background:var(--surface-1);border:1px solid var(--border);
  color:var(--text-3);cursor:pointer;
  transition:border-color .2s var(--ease),color .2s var(--ease),background .2s var(--ease);
}
.bar-btn:hover{border-color:var(--border-h);color:var(--text)}

/* ── Feature 3: Sort Toggle ──────────────────────── */
.bar-sep{width:1px;height:18px;background:var(--border);margin:0 4px}
.bar-lbl{font-size:.72rem;color:var(--text-3);text-transform:uppercase;letter-spacing:.06em;white-space:nowrap}
.sort-btn{
  padding:5px 10px;border-radius:100px;font-size:.74rem;
  background:var(--surface-1);border:1px solid var(--border);
  color:var(--text-2);cursor:pointer;font-family:var(--font);
  transition:all .18s var(--ease);white-space:nowrap;
}
.sort-btn:hover{border-color:var(--border-h);color:var(--text)}
.sort-btn.active{background:var(--indigo);border-color:var(--indigo);color:#fff}

/* ── Feature 4: Search Highlighting ──────────────── */
mark{background:rgba(251,191,36,.25);color:inherit;border-radius:2px;padding:0 1px}
:root[data-theme="light"] mark{background:rgba(251,191,36,.35)}

/* ── Feature 5: Copy Deep-Link Button ────────────── */
.t-link-btn{
  display:inline-flex;align-items:center;justify-content:center;
  width:24px;height:24px;border-radius:4px;
  background:transparent;border:none;color:var(--text-3);
  cursor:pointer;opacity:0;transition:opacity .15s var(--ease),color .15s var(--ease);
  flex-shrink:0;position:relative;
}
.t-row:hover .t-link-btn{opacity:.6}
.t-link-btn:hover{opacity:1!important;color:var(--indigo)}
.t-link-copied{
  position:absolute;bottom:100%;left:50%;transform:translateX(-50%);
  padding:2px 8px;border-radius:4px;font-size:.68rem;white-space:nowrap;
  background:var(--surface-3);color:var(--text);pointer-events:none;
  animation:fade-up .2s var(--ease);
}

/* ── Feature 6: Diff-Friendly Error Display ──────── */
.err-expected{color:var(--emerald);font-weight:600}
.err-actual{color:var(--rose);font-weight:600}

/* ── Feature 7: Keyboard Navigation ──────────────── */
.t-row.kb-focus{outline:2px solid var(--indigo);outline-offset:-2px;background:var(--indigo-d)}
.kb-overlay{position:fixed;inset:0;z-index:9998;background:rgba(0,0,0,.5);backdrop-filter:blur(4px);display:flex;align-items:center;justify-content:center}
.kb-modal{
  background:var(--surface-0);border:1px solid var(--border);border-radius:var(--r-lg);
  padding:24px 28px;max-width:380px;width:90%;box-shadow:0 24px 48px rgba(0,0,0,.3);
}
.kb-modal h3{font-size:1rem;font-weight:700;margin-bottom:14px;color:var(--text)}
.kb-row{display:flex;justify-content:space-between;align-items:center;padding:5px 0;font-size:.84rem;color:var(--text-2)}
.kb-key{
  display:inline-flex;align-items:center;justify-content:center;
  min-width:24px;height:24px;padding:0 7px;border-radius:4px;
  background:var(--surface-2);border:1px solid var(--border);
  font-family:var(--mono);font-size:.74rem;font-weight:600;color:var(--text);
}
:root[data-theme="light"] .kb-overlay{background:rgba(0,0,0,.3)}

/* ── Feature 8: Sticky Mini-Header ───────────────── */
.sticky-bar{
  position:sticky;top:0;z-index:100;
  display:flex;align-items:center;gap:12px;
  padding:8px 24px;
  background:rgba(11,13,17,.85);backdrop-filter:blur(12px);
  border-bottom:1px solid var(--border);
  transform:translateY(-100%);opacity:0;
  transition:transform .25s var(--ease),opacity .25s var(--ease);
}
.sticky-bar.visible{transform:none;opacity:1}
.sticky-name{font-size:.84rem;font-weight:700;color:var(--text);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.sticky-badges{display:flex;gap:6px;flex-shrink:0}
.sticky-b{
  font-size:.7rem;font-weight:700;padding:2px 8px;border-radius:100px;
  font-variant-numeric:tabular-nums;
}
.sb-pass{background:var(--emerald-d);color:var(--emerald)}
.sb-fail{background:var(--rose-d);color:var(--rose)}
.sb-skip{background:var(--amber-d);color:var(--amber)}
.sticky-search-btn{
  margin-left:auto;display:flex;align-items:center;justify-content:center;
  width:28px;height:28px;border-radius:var(--r);
  background:var(--surface-2);border:1px solid var(--border);
  color:var(--text-3);cursor:pointer;
  transition:color .15s var(--ease),border-color .15s var(--ease);
}
.sticky-search-btn:hover{color:var(--text);border-color:var(--border-h)}
:root[data-theme="light"] .sticky-bar{background:rgba(248,249,251,.85)}

/* ── Feature 9: 100% Pass Celebration ────────────── */
@keyframes shimmer{
  0%{background-position:200% 0}
  100%{background-position:-200% 0}
}
@keyframes ring-glow{
  0%,100%{filter:drop-shadow(0 0 6px rgba(52,211,153,.3))}
  50%{filter:drop-shadow(0 0 16px rgba(52,211,153,.6))}
}
.dash.celebrate .stat{
  background-image:linear-gradient(90deg,transparent 30%,rgba(52,211,153,.06) 50%,transparent 70%);
  background-size:200% 100%;animation:shimmer 3s linear infinite;
}
.dash.celebrate .ring{animation:ring-glow 2.5s ease-in-out infinite}
@media(prefers-reduced-motion:reduce){
  .dash.celebrate .stat,.dash.celebrate .ring{animation:none}
}

/* ── Feature 10: Duration Histogram ──────────────── */
.dur-hist{display:flex;align-items:flex-end;gap:2px;height:36px;margin-top:8px}
.dur-hist-bar{
  flex:1;min-width:0;border-radius:2px 2px 0 0;
  background:linear-gradient(to top,var(--indigo),rgba(129,140,248,.5));
  position:relative;cursor:default;
  transition:filter .15s var(--ease);
}
.dur-hist-bar:hover{filter:brightness(1.3)}
.dur-hist-bar::after{
  content:attr(data-tip);
  position:absolute;bottom:100%;left:50%;transform:translateX(-50%);
  padding:3px 8px;border-radius:4px;font-size:.66rem;white-space:nowrap;
  background:var(--surface-3);color:var(--text);pointer-events:none;
  opacity:0;transition:opacity .15s var(--ease);
}
.dur-hist-bar:hover::after{opacity:1}

/* ── Lazy Sentinel ──────────────────────────────── */
.lazy-sentinel{display:flex;align-items:center;justify-content:center;padding:16px;color:var(--text-3);font-size:.82rem}

/* ── Accessibility: Skip Link ────────────────────── */
.skip-link{
  position:absolute;top:-100%;left:16px;
  padding:8px 16px;border-radius:var(--r);
  background:var(--indigo);color:#fff;font-size:.84rem;font-weight:600;
  z-index:10000;text-decoration:none;
  transition:top .2s var(--ease);
}
.skip-link:focus{top:8px}

/* ── Accessibility: Focus-Visible ────────────────── */
:focus-visible{outline:2px solid var(--indigo);outline-offset:2px;border-radius:var(--r)}
.pill:focus-visible,.sort-btn:focus-visible{outline-offset:0}
.search input:focus-visible{outline:none} /* uses custom box-shadow instead */
.t-row:focus-visible{outline-offset:-2px}

/* ── Accessibility: Touch Targets ────────────────── */
@media(pointer:coarse){
  .theme-btn{width:44px;height:44px}
  .bar-btn{width:40px;height:40px}
  .pill{padding:10px 16px}
  .sort-btn{padding:8px 14px}
  .t-row{padding:12px 16px 12px 20px;min-height:44px}
  .t-link-btn{width:36px;height:36px;opacity:.5}
  .grp-hd{padding:12px 16px;min-height:44px}
  .sticky-search-btn{width:36px;height:36px}
}

/* ── Accessibility: Contrast Boost ───────────────── */
/* Dark theme: bump secondary/tertiary text to meet WCAG AA */
:root{
  --text-2:#a8aebb;
  --text-3:#717a8c;
}
:root[data-theme="light"]{
  --text-2:#4a5060;
  --text-3:#6b7280;
}

/* ── Accessibility: Sort Group ───────────────────── */
.sort-group{display:flex;gap:4px;align-items:center}
.grp-toggle{display:flex;gap:4px;align-items:center}

/* ── Mobile Improvements ─────────────────────────── */
@media(max-width:768px){
  .bar-actions{width:100%;justify-content:flex-end}
  .bar-sep{display:none}
  .sticky-bar{padding:8px 12px;gap:8px}
  .sticky-name{font-size:.78rem;max-width:120px}
}
@media(max-width:480px){
  .pills{flex-wrap:wrap}
  .pill .pill-count{display:none}
  .sort-group{flex-wrap:wrap}
  .grp-toggle{flex-wrap:wrap}
}

/* ── Print Improvements ──────────────────────────── */
@media print{
  .skip-link,.sticky-bar,.bar-actions,.t-link-btn,.search,.copy-btn,.theme-btn{display:none!important}
  .grp{break-inside:avoid}
  .t-row{break-inside:avoid}
  .t-badge{-webkit-print-color-adjust:exact;print-color-adjust:exact}
  .grp-b{-webkit-print-color-adjust:exact;print-color-adjust:exact}
  .dot{-webkit-print-color-adjust:exact;print-color-adjust:exact}
  .ring-seg{stroke-opacity:1!important}
}

/* ── High Contrast Mode ──────────────────────────── */
@media(forced-colors:active){
  .pill.active{border:2px solid LinkText}
  .sort-btn.active{border:2px solid LinkText}
  .t-badge{border:1px solid CanvasText}
  .grp-indicator{forced-color-adjust:none}
  .t-row.kb-focus{outline:2px solid LinkText}
}
""";
    }

    private static string GetJavaScript()
    {
        return """
(function(){
'use strict';
const raw = document.getElementById('test-data');
if (!raw) return;
const data = JSON.parse(raw.textContent);
const groups = data.groups || [];
const spans = data.spans || [];
const container = document.getElementById('testGroups');
const searchInput = document.getElementById('searchInput');
const clearBtn = document.getElementById('clearSearch');
const filterBtns = document.getElementById('filterButtons');
const filterSummary = document.getElementById('filterSummary');
let activeFilter = 'all';
let searchText = '';
let debounceTimer;
let sortMode = 'default';
let groupMode = 'class';
let renderLimit = 20;
let kbIdx = -1;

const spansByTrace = {};
const bySpanId = {};
spans.forEach(s => {
    if (!spansByTrace[s.traceId]) spansByTrace[s.traceId] = [];
    spansByTrace[s.traceId].push(s);
    bySpanId[s.spanId] = s;
});

// Build suite span lookup: className -> span
const suiteSpanByClass = {};
spans.forEach(s => {
    if (!s.name.startsWith('test suite')) return;
    const tag = (s.tags||[]).find(t => t.key === 'test.suite.name');
    if (tag) suiteSpanByClass[tag.value] = s;
});

function matchesFilter(t) {
    if (activeFilter !== 'all') {
        if (activeFilter === 'failed') {
            if (t.status !== 'failed' && t.status !== 'error' && t.status !== 'timedOut') return false;
        } else if (t.status !== activeFilter) return false;
    }
    if (searchText) {
        const q = searchText.toLowerCase();
        const h = (t.displayName + ' ' + t.className + ' ' + (t.categories||[]).join(' ') + ' ' + (t.traceId||'') + ' ' + (t.spanId||'')).toLowerCase();
        if (!h.includes(q)) return false;
    }
    return true;
}

function fmt(ms) {
    if (ms < 1) return '<1ms';
    if (Math.round(ms) < 1000) return Math.round(ms) + 'ms';
    if (ms < 60000) return (ms/1000).toFixed(2) + 's';
    return (ms/60000).toFixed(1) + 'm';
}

function esc(s) {
    if (!s) return '';
    const d = document.createElement('div');
    d.textContent = s;
    return d.innerHTML;
}

// Feature 4: Search highlight helper
function highlight(text, query) {
    if (!query) return esc(text);
    const escaped = esc(text);
    const re = new RegExp('(' + query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + ')', 'gi');
    return escaped.replace(re, '<mark>$1</mark>');
}

// Feature 3: Sort comparator
const statusOrder = {failed:0,error:0,timedOut:1,inProgress:2,unknown:3,passed:4,skipped:5,cancelled:6};
function sortTests(tests) {
    if (sortMode === 'duration') return [...tests].sort((a,b) => b.durationMs - a.durationMs);
    if (sortMode === 'name') return [...tests].sort((a,b) => a.displayName.localeCompare(b.displayName));
    return [...tests].sort((a,b) => (statusOrder[a.status]||9) - (statusOrder[b.status]||9));
}

function computeDisplayGroups() {
    if (groupMode === 'namespace') {
        const map = {};
        groups.forEach(function(g) {
            const ns = g.namespace || '(no namespace)';
            if (!map[ns]) map[ns] = {label: ns, tests: [], className: null, namespace: ns};
            g.tests.forEach(function(t) { map[ns].tests.push(t); });
        });
        return Object.values(map);
    }
    if (groupMode === 'status') {
        const buckets = {Failed:[],Passed:[],Skipped:[],Cancelled:[]};
        groups.forEach(function(g) {
            g.tests.forEach(function(t) {
                if (t.status==='failed'||t.status==='error'||t.status==='timedOut') buckets.Failed.push(t);
                else if (t.status==='passed') buckets.Passed.push(t);
                else if (t.status==='skipped') buckets.Skipped.push(t);
                else buckets.Cancelled.push(t);
            });
        });
        var out = [];
        ['Failed','Passed','Skipped','Cancelled'].forEach(function(k) {
            if (buckets[k].length) out.push({label: k, tests: buckets[k], className: null});
        });
        return out;
    }
    return groups.map(function(g) { return {label: g.className, tests: g.tests, className: g.className, namespace: g.namespace}; });
}

// Feature 6: Diff-friendly error formatting
function formatAssertionMessage(msg) {
    if (!msg) return '';
    let s = esc(msg);
    // Pattern: "Expected: X\nActual: Y" or "Expected: X\r\nActual: Y"
    s = s.replace(/^(Expected:\s*)(.+)$/gm, '$1<span class="err-expected">$2</span>');
    s = s.replace(/^(Actual:\s*)(.+)$/gm, '$1<span class="err-actual">$2</span>');
    // Pattern: "expected X but was Y"
    s = s.replace(/(expected\s+)(.+?)(\s+but was\s+)(.+)/gi, '$1<span class="err-expected">$2</span>$3<span class="err-actual">$4</span>');
    return s;
}

// Feature 5: Link icon SVG
const linkIcon = '<svg viewBox="0 0 16 16" fill="currentColor" width="14" height="14"><path d="M7.775 3.275a.75.75 0 0 0 1.06 1.06l1.25-1.25a2 2 0 1 1 2.83 2.83l-2.5 2.5a2 2 0 0 1-2.83 0 .75.75 0 0 0-1.06 1.06 3.5 3.5 0 0 0 4.95 0l2.5-2.5a3.5 3.5 0 0 0-4.95-4.95l-1.25 1.25Zm-.8 9.45a.75.75 0 0 0-1.06-1.06l-1.25 1.25a2 2 0 0 1-2.83-2.83l2.5-2.5a2 2 0 0 1 2.83 0 .75.75 0 0 0 1.06-1.06 3.5 3.5 0 0 0-4.95 0l-2.5 2.5a3.5 3.5 0 0 0 4.95 4.95l1.25-1.25Z"/></svg>';

const arrow = '<svg class="grp-arrow" viewBox="0 0 16 16" fill="currentColor"><path d="M6.22 4.22a.75.75 0 0 1 1.06 0l3.25 3.25a.75.75 0 0 1 0 1.06l-3.25 3.25a.75.75 0 0 1-1.06-1.06L8.94 8 6.22 5.28a.75.75 0 0 1 0-1.06Z"/></svg>';

function fmtTime(iso) {
    if (!iso) return '—';
    const d = new Date(iso);
    return d.toLocaleTimeString([], {hour:'2-digit',minute:'2-digit',second:'2-digit',fractionalSecondDigits:3});
}

const copyIcon = '<svg viewBox="0 0 16 16" fill="currentColor"><path d="M0 6.75C0 5.784.784 5 1.75 5h1.5a.75.75 0 0 1 0 1.5h-1.5a.25.25 0 0 0-.25.25v7.5c0 .138.112.25.25.25h7.5a.25.25 0 0 0 .25-.25v-1.5a.75.75 0 0 1 1.5 0v1.5A1.75 1.75 0 0 1 9.25 16h-7.5A1.75 1.75 0 0 1 0 14.25Z"/><path d="M5 1.75C5 .784 5.784 0 6.75 0h7.5C15.216 0 16 .784 16 1.75v7.5A1.75 1.75 0 0 1 14.25 11h-7.5A1.75 1.75 0 0 1 5 9.25Zm1.75-.25a.25.25 0 0 0-.25.25v7.5c0 .138.112.25.25.25h7.5a.25.25 0 0 0 .25-.25v-7.5a.25.25 0 0 0-.25-.25Z"/></svg>';
const checkIcon = '<svg viewBox="0 0 16 16" fill="currentColor"><path d="M13.78 4.22a.75.75 0 0 1 0 1.06l-7.25 7.25a.75.75 0 0 1-1.06 0L2.22 9.28a.75.75 0 0 1 1.06-1.06L6 10.94l6.72-6.72a.75.75 0 0 1 1.06 0Z"/></svg>';

function wrapPre(content, cls) {
    return '<div class="d-pre-wrap"><div class="d-pre'+(cls?' '+cls:'')+'">' + content + '</div><button class="copy-btn" title="Copy to clipboard">'+copyIcon+'</button></div>';
}

function renderDetail(t) {
    let h = '';

    // Summary info row
    h += '<div class="d-sec d-info">';
    h += '<span class="d-info-item"><span class="d-info-label">Started</span> ' + fmtTime(t.startTime) + '</span>';
    h += '<span class="d-info-item"><span class="d-info-label">Ended</span> ' + fmtTime(t.endTime) + '</span>';
    h += '<span class="d-info-item"><span class="d-info-label">Duration</span> ' + fmt(t.durationMs) + '</span>';
    if (t.retryAttempt > 0) {
        h += '<span class="d-info-item"><span class="retry-tag">Retry #'+t.retryAttempt+'</span></span>';
    }
    h += '</div>';

    if (t.exception) {
        h += '<div class="d-sec"><div class="d-lbl">Exception</div>';
        h += wrapPre(esc(t.exception.type) + ': ' + formatAssertionMessage(t.exception.message), 'err');
        if (t.exception.stackTrace) h += wrapPre(esc(t.exception.stackTrace), 'stack');
        let inner = t.exception.innerException;
        while (inner) {
            h += '<div class="d-lbl" style="margin-top:8px">Inner Exception</div>';
            h += wrapPre(esc(inner.type) + ': ' + formatAssertionMessage(inner.message), 'err');
            if (inner.stackTrace) h += wrapPre(esc(inner.stackTrace), 'stack');
            inner = inner.innerException;
        }
        h += '</div>';
    }
    if (t.skipReason) {
        h += '<div class="d-sec"><div class="d-lbl">Skip Reason</div>';
        h += wrapPre(esc(t.skipReason)) + '</div>';
    }
    if (t.output) {
        h += '<div class="d-sec d-collapsible"><div class="d-collapse-toggle">' + tlArrow + 'Standard Output</div>';
        h += '<div class="d-collapse-content"><div class="d-collapse-inner">' + wrapPre(esc(t.output)) + '</div></div></div>';
    }
    if (t.errorOutput) {
        h += '<div class="d-sec d-collapsible"><div class="d-collapse-toggle">' + tlArrow + 'Error Output</div>';
        h += '<div class="d-collapse-content"><div class="d-collapse-inner">' + wrapPre(esc(t.errorOutput), 'err') + '</div></div></div>';
    }
    if (t.categories && t.categories.length > 0) {
        h += '<div class="d-sec"><div class="d-lbl">Categories</div><div class="d-tags">';
        t.categories.forEach(c => { h += '<span class="d-tag">'+esc(c)+'</span>'; });
        h += '</div></div>';
    }
    if (t.customProperties && t.customProperties.length > 0) {
        h += '<div class="d-sec"><div class="d-lbl">Properties</div><div class="d-tags">';
        t.customProperties.forEach(p => { h += '<span class="d-tag kv">'+esc(p.key)+'='+esc(p.value)+'</span>'; });
        h += '</div></div>';
    }
    if (t.filePath) {
        h += '<div class="d-sec"><span class="d-src">'+esc(t.filePath);
        if (t.lineNumber) h += ':'+t.lineNumber;
        h += '</span></div>';
    }
    if (t.traceId && t.spanId && spansByTrace[t.traceId]) h += renderTrace(t.traceId, t.spanId);
    return h;
}

// Collect descendants of a span within a trace
function getDescendants(traceSpans, rootId) {
    const children = {};
    traceSpans.forEach(s => {
        if (s.parentSpanId) {
            if (!children[s.parentSpanId]) children[s.parentSpanId] = [];
            children[s.parentSpanId].push(s.spanId);
        }
    });
    const included = new Set();
    function walk(sid) {
        if (included.has(sid)) return;
        included.add(sid);
        (children[sid] || []).forEach(walk);
    }
    walk(rootId);
    return traceSpans.filter(s => included.has(s.spanId));
}

// Render a span waterfall from a filtered list of spans
function renderSpanRows(sp, uid) {
    if (!sp || !sp.length) return '';
    const mn = Math.min(...sp.map(s => s.startTimeMs));
    const mx = Math.max(...sp.map(s => s.startTimeMs + s.durationMs));
    const dur = mx - mn || 1;
    const idSet = new Set(sp.map(s => s.spanId));
    const depth = {};
    function gd(s) {
        if (depth[s.spanId] !== undefined) return depth[s.spanId];
        if (!s.parentSpanId || !bySpanId[s.parentSpanId] || !idSet.has(s.parentSpanId)) { depth[s.spanId] = 0; return 0; }
        depth[s.spanId] = gd(bySpanId[s.parentSpanId]) + 1; return depth[s.spanId];
    }
    sp.forEach(gd);
    const sorted = [...sp].sort((a, b) => a.startTimeMs - b.startTimeMs);
    let h = '<div class="trace">';
    sorted.forEach((s, i) => {
        const d = depth[s.spanId] || 0;
        const l = ((s.startTimeMs - mn) / dur * 100).toFixed(2);
        const w = Math.max((s.durationMs / dur * 100), .5).toFixed(2);
        const cls = s.status === 'Error' ? 'err' : s.status === 'Ok' ? 'ok' : 'unk';
        h += '<div class="sp-row" data-si="' + i + '">';
        h += '<span class="sp-indent" style="width:' + (d * 14) + 'px"></span>';
        h += '<div class="sp-bar ' + cls + '" style="margin-left:' + l + '%;width:' + w + '%" title="' + esc(s.name) + ' (' + fmt(s.durationMs) + ')"></div>';
        h += '<span class="sp-name">' + esc(s.name) + '</span>';
        h += '<span class="sp-dur">' + fmt(s.durationMs) + '</span>';
        h += '</div>';
        let ex = '<div class="sp-extra" id="sp-' + uid + '-' + i + '">';
        ex += '<strong>Source:</strong> ' + esc(s.source) + ' &middot; <strong>Kind:</strong> ' + esc(s.kind);
        if (s.tags && s.tags.length) { ex += '<br><strong>Tags:</strong> '; s.tags.forEach(t => { ex += esc(t.key) + '=' + esc(t.value) + ' '; }); }
        if (s.events && s.events.length) { ex += '<br><strong>Events:</strong> '; s.events.forEach(e => { ex += esc(e.name) + ' '; if (e.tags) e.tags.forEach(t => { ex += esc(t.key) + '=' + esc(t.value) + ' '; }); }); }
        ex += '</div>';
        h += ex;
    });
    h += '</div>';
    return h;
}

// Per-test trace: include parent suite span for context, then test case span + its descendants
function renderTrace(tid, rootSpanId) {
    const allSpans = spansByTrace[tid];
    if (!allSpans || !allSpans.length) return '';
    const sp = getDescendants(allSpans, rootSpanId);
    if (!sp.length) return '';
    // Include the parent suite span so the test bar is shown relative to the class duration
    const root = bySpanId[rootSpanId];
    if (root && root.parentSpanId && bySpanId[root.parentSpanId]) {
        const parent = bySpanId[root.parentSpanId];
        if (!sp.some(s => s.spanId === parent.spanId)) {
            sp.unshift(parent);
        }
    }
    return '<div class="d-sec"><div class="d-lbl">Trace Timeline</div>' + renderSpanRows(sp, 't-' + rootSpanId) + '</div>';
}

const tlArrow = '<svg class="tl-arrow" width="12" height="12" viewBox="0 0 16 16" fill="currentColor"><path d="M6.22 4.22a.75.75 0 0 1 1.06 0l3.25 3.25a.75.75 0 0 1 0 1.06l-3.25 3.25a.75.75 0 0 1-1.06-1.06L8.94 8 6.22 5.28a.75.75 0 0 1 0-1.06Z"/></svg>';

// Suite-level trace: test suite span + non-test-case children (hooks, setup, teardown)
function renderClassSummary(g, ft) {
    // Compute start/end from suite span if available, else from tests
    const suite = suiteSpanByClass[g.className];
    let startIso = null, endIso = null, durMs = 0;
    if (suite) {
        // Suite span times are relative ms — use the earliest test's startTime as anchor
        durMs = suite.durationMs;
    }
    // Derive from test timestamps
    let minStart = null, maxEnd = null;
    ft.forEach(function(t){
        if (t.startTime) {
            const s = new Date(t.startTime);
            if (!minStart || s < minStart) minStart = s;
        }
        if (t.endTime) {
            const e = new Date(t.endTime);
            if (!maxEnd || e > maxEnd) maxEnd = e;
        }
    });
    startIso = minStart;
    endIso = maxEnd;
    if (!durMs && minStart && maxEnd) durMs = maxEnd - minStart;

    let h = '<div class="grp-summary">';
    h += '<span><span class="d-info-label">Started</span> ' + (startIso ? fmtTime(startIso.toISOString()) : '\u2014') + '</span>';
    h += '<span><span class="d-info-label">Ended</span> ' + (endIso ? fmtTime(endIso.toISOString()) : '\u2014') + '</span>';
    h += '<span><span class="d-info-label">Duration</span> <span class="grp-sum-dur">' + fmt(durMs) + '</span></span>';
    h += '<span><span class="d-info-label">Tests</span> ' + ft.length + '</span>';
    h += '</div>';
    return h;
}

function renderSuiteTrace(className) {
    const suite = suiteSpanByClass[className];
    if (!suite) return '';
    const allSpans = spansByTrace[suite.traceId];
    if (!allSpans) return '';
    const all = getDescendants(allSpans, suite.spanId);
    const testCaseIds = new Set();
    all.forEach(s => { if (s.name.startsWith('test case')) testCaseIds.add(s.spanId); });
    const tcDescendants = new Set();
    testCaseIds.forEach(id => { getDescendants(all, id).forEach(s => { if (s.spanId !== id) tcDescendants.add(s.spanId); }); });
    const filtered = all.filter(s => !tcDescendants.has(s.spanId) && !testCaseIds.has(s.spanId));
    // Include parent spans (assembly, session) for context
    let ancestor = suite.parentSpanId ? bySpanId[suite.parentSpanId] : null;
    while (ancestor) {
        if (!filtered.some(s => s.spanId === ancestor.spanId)) filtered.unshift(ancestor);
        ancestor = ancestor.parentSpanId ? bySpanId[ancestor.parentSpanId] : null;
    }
    if (filtered.length <= 1) return '';
    return '<div class="suite-trace"><div class="tl-toggle">' + tlArrow + 'Class Timeline</div><div class="tl-content"><div class="tl-content-inner"><div class="tl-content-pad">' + renderSpanRows(filtered, 'suite-' + className) + '</div></div></div></div>';
}

// Global timeline: session + assembly + suite spans
function renderGlobalTimeline() {
    const topSpans = spans.filter(s => s.name.startsWith('test session') || s.name.startsWith('test assembly') || s.name.startsWith('test suite'));
    if (!topSpans.length) return '';
    return '<div class="global-trace"><div class="tl-toggle">' + tlArrow + 'Execution Timeline</div><div class="tl-content"><div class="tl-content-inner"><div class="tl-content-pad">' + renderSpanRows(topSpans, 'global') + '</div></div></div></div>';
}

function renderFailedSection() {
    const sec = document.getElementById('failedSection');
    if (!sec) return;
    const failed = [];
    groups.forEach(function(g){
        g.tests.forEach(function(t){
            if (t.status==='failed'||t.status==='error'||t.status==='timedOut') failed.push({t:t,cls:g.className});
        });
    });
    if (!failed.length) { sec.innerHTML=''; return; }
    let h = '<div class="qa-section tl-open"><div class="tl-toggle">'+tlArrow+' Failed Tests ('+failed.length+')</div><div class="tl-content"><div class="tl-content-inner"><div class="tl-content-pad">';
    failed.forEach(function(f){
        const errMsg = f.t.exception ? (f.t.exception.type+': '+f.t.exception.message) : '';
        const truncErr = errMsg.length > 120 ? errMsg.substring(0,120)+'…' : errMsg;
        h += '<div class="qa-item" data-scroll-tid="'+f.t.id+'">';
        h += '<span class="t-badge '+f.t.status+'">'+esc(f.t.status)+'</span>';
        h += '<div class="qa-info"><div class="qa-info-name">'+esc(f.t.displayName)+'</div>';
        h += '<div class="qa-info-class">'+esc(f.cls)+'</div></div>';
        if (truncErr) h += '<span class="qa-err" title="'+esc(errMsg)+'">'+esc(truncErr)+'</span>';
        h += '<span class="qa-dur">'+fmt(f.t.durationMs)+'</span>';
        h += '</div>';
    });
    h += '</div></div></div></div>';
    sec.innerHTML = h;
}

function renderSlowestSection() {
    const sec = document.getElementById('slowestSection');
    if (!sec) return;
    const all = [];
    groups.forEach(function(g){
        g.tests.forEach(function(t){ all.push({t:t,cls:g.className}); });
    });
    all.sort(function(a,b){ return b.t.durationMs - a.t.durationMs; });
    const top = all.slice(0,10);
    if (!top.length) { sec.innerHTML=''; return; }
    const maxMs = top[0].t.durationMs || 1;
    let h = '<div class="qa-section"><div class="tl-toggle">'+tlArrow+' Top 10 Slowest Tests</div><div class="tl-content"><div class="tl-content-inner"><div class="tl-content-pad">';
    top.forEach(function(f,i){
        const pct = Math.max((f.t.durationMs/maxMs)*100,1).toFixed(1);
        h += '<div class="qa-item" data-scroll-tid="'+f.t.id+'">';
        h += '<span class="slow-rank">#'+(i+1)+'</span>';
        h += '<div class="qa-info"><div class="qa-info-name">'+esc(f.t.displayName)+'</div>';
        h += '<div class="qa-info-class">'+esc(f.cls)+'</div></div>';
        h += '<div class="slow-bar-track"><div class="slow-bar-fill" style="width:'+pct+'%"></div></div>';
        h += '<span class="qa-dur">'+fmt(f.t.durationMs)+'</span>';
        h += '</div>';
    });
    h += '</div></div></div></div>';
    sec.innerHTML = h;
}

function render() {
    let total = 0;
    let html = '';
    const displayGroups = computeDisplayGroups();
    const limited = displayGroups.slice(0, renderLimit);
    limited.forEach((g,gi)=>{
        const ft = sortTests(g.tests.filter(matchesFilter));
        if (!ft.length) return;
        total += ft.length;
        const fail = ft.some(t=>t.status==='failed'||t.status==='error'||t.status==='timedOut');
        const open = fail || searchText;
        const c = {p:0,f:0,s:0};
        ft.forEach(t=>{
            if(t.status==='passed')c.p++;
            else if(t.status==='failed'||t.status==='error'||t.status==='timedOut')c.f++;
            else if(t.status==='skipped')c.s++;
        });
        html += '<div class="grp'+(open?' open':'')+'" data-gi="'+gi+'">';
        html += '<div class="grp-hd'+(fail?' fail':'')+'" role="button" tabindex="0" aria-expanded="'+(open?'true':'false')+'">';
        html += '<div class="grp-indicator"></div>';
        html += arrow;
        html += '<span class="grp-name">'+(searchText?highlight(g.label,searchText):esc(g.label))+'</span>';
        html += '<span class="grp-badges">';
        if(c.p) html += '<span class="grp-b gp">'+c.p+'</span>';
        if(c.f) html += '<span class="grp-b gf">'+c.f+'</span>';
        if(c.s) html += '<span class="grp-b gs">'+c.s+'</span>';
        html += '<span class="grp-b gt">'+ft.length+'</span>';
        html += '</span></div>';
        html += '<div class="grp-body"><div class="grp-body-inner"><div class="grp-body-pad">';
        if (groupMode === 'class') {
            html += renderClassSummary(g, ft);
            html += renderSuiteTrace(g.className);
        }
        ft.forEach((t,ti)=>{
            html += '<div class="t-row" id="test-'+t.id+'" data-gi="'+gi+'" data-ti="'+ti+'" data-tid="'+t.id+'" style="--row-idx:'+Math.min(ti,7)+'">';
            html += '<span class="t-badge '+t.status+'">'+esc(t.status)+'</span>';
            html += '<span class="t-name">'+(searchText?highlight(t.displayName,searchText):esc(t.displayName))+'</span>';
            if(t.retryAttempt>0) html += '<span class="retry-tag">retry '+t.retryAttempt+'</span>';
            html += '<button class="t-link-btn" data-link-tid="'+t.id+'" title="Copy link">'+linkIcon+'</button>';
            html += '<span class="t-dur">'+fmt(t.durationMs)+'</span>';
            html += '</div>';
            html += '<div class="t-detail" data-gi="'+gi+'" data-ti="'+ti+'"><div class="t-detail-inner"><div class="t-detail-pad">';
            html += renderDetail(t);
            html += '</div></div></div>';
        });
        html += '</div></div></div></div>';
    });
    if (displayGroups.length > renderLimit) {
        html += '<div id="lazySentinel" class="lazy-sentinel">Loading more\u2026</div>';
    }
    container.innerHTML = html;
    observeSentinel();
    filterSummary.textContent = (activeFilter!=='all'||searchText)
        ? 'Showing '+total+' of '+data.summary.total+' tests' : '';
    kbIdx = -1;
}

function syncHash() {
    const p = [];
    if (activeFilter !== 'all') p.push('filter=' + encodeURIComponent(activeFilter));
    if (sortMode !== 'default') p.push('sort=' + encodeURIComponent(sortMode));
    if (searchText) p.push('search=' + encodeURIComponent(searchText));
    if (groupMode !== 'class') p.push('group=' + encodeURIComponent(groupMode));
    history.replaceState(null, '', p.length ? '#' + p.join('&') : location.pathname);
}

function loadFromHash() {
    const h = location.hash;
    if (!h || h.length < 2) return;
    const raw = h.substring(1);
    if (raw.startsWith('test-')) return; // deep-link takes priority
    const pairs = raw.split('&');
    pairs.forEach(function(pair) {
        const eq = pair.indexOf('=');
        if (eq < 0) return;
        const k = decodeURIComponent(pair.substring(0, eq));
        const v = decodeURIComponent(pair.substring(eq + 1));
        if (k === 'filter') activeFilter = v;
        else if (k === 'sort') sortMode = v;
        else if (k === 'search') { searchText = v; searchInput.value = v; clearBtn.style.display = v ? 'block' : 'none'; }
        else if (k === 'group') groupMode = v;
    });
    // Sync button active states
    filterBtns.querySelectorAll('.pill').forEach(function(b) {
        const isActive = b.dataset.filter === activeFilter;
        b.classList.toggle('active', isActive);
        b.setAttribute('aria-pressed', isActive ? 'true' : 'false');
    });
    document.querySelectorAll('.sort-group .sort-btn').forEach(function(b) {
        const isActive = b.dataset.sort === sortMode;
        b.classList.toggle('active', isActive);
        b.setAttribute('aria-checked', isActive ? 'true' : 'false');
    });
    document.querySelectorAll('.grp-toggle .sort-btn').forEach(function(b) {
        const isActive = b.dataset.group === groupMode;
        b.classList.toggle('active', isActive);
        b.setAttribute('aria-checked', isActive ? 'true' : 'false');
    });
}

let lazyObs = null;
function observeSentinel() {
    if (lazyObs) lazyObs.disconnect();
    const el = document.getElementById('lazySentinel');
    if (!el) return;
    lazyObs = new IntersectionObserver(function(entries) {
        if (entries[0].isIntersecting) { renderLimit += 20; render(); }
    }, {rootMargin: '200px'});
    lazyObs.observe(el);
}

function scrollToTest(testId) {
    const row = document.getElementById('test-' + testId);
    if (!row) return;
    // Expand parent group
    const grp = row.closest('.grp');
    if (grp && !grp.classList.contains('open')) grp.classList.add('open');
    // Expand detail panel
    const det = row.nextElementSibling;
    if (det && det.classList.contains('t-detail') && !det.classList.contains('open')) det.classList.add('open');
    // Scroll into view
    row.scrollIntoView({behavior:'smooth',block:'center'});
    // Flash highlight
    row.classList.add('qa-highlight');
    setTimeout(function(){row.classList.remove('qa-highlight');},1500);
    // Update hash
    history.replaceState(null,'','#test-'+testId);
}

function checkHash() {
    const h = location.hash;
    if (h && h.startsWith('#test-')) {
        const testId = h.substring(6);
        setTimeout(function(){scrollToTest(testId);},100);
    }
}

// Toggle for collapsible sections (timelines & output panels)
document.addEventListener('click',function(e){
    const tl = e.target.closest('.tl-toggle');
    if(tl){tl.parentElement.classList.toggle('tl-open');return;}
    const ct = e.target.closest('.d-collapse-toggle');
    if(ct){ct.parentElement.classList.toggle('d-col-open');return;}
});

container.addEventListener('click',function(e){
    // Feature 5: Deep-link copy button
    const lb = e.target.closest('.t-link-btn');
    if(lb){
        e.stopPropagation();
        const tid = lb.dataset.linkTid;
        const url = location.origin + location.pathname + '#test-' + tid;
        navigator.clipboard.writeText(url).then(function(){
            const tip = document.createElement('span');
            tip.className='t-link-copied';tip.textContent='Copied!';
            lb.appendChild(tip);
            setTimeout(function(){tip.remove();},1200);
        });
        return;
    }
    const hd = e.target.closest('.grp-hd');
    if(hd){const grp=hd.parentElement;grp.classList.toggle('open');hd.setAttribute('aria-expanded',grp.classList.contains('open')?'true':'false');return;}
    const row = e.target.closest('.t-row');
    if(row){
        const det = container.querySelector('.t-detail[data-gi="'+row.dataset.gi+'"][data-ti="'+row.dataset.ti+'"]');
        if(det) det.classList.toggle('open');
        if(row.dataset.tid) history.replaceState(null,'','#test-'+row.dataset.tid);
        return;
    }
    const sr = e.target.closest('.sp-row');
    if(sr){const nx=sr.nextElementSibling;if(nx&&nx.classList.contains('sp-extra'))nx.classList.toggle('open');}
});

filterBtns.addEventListener('click',function(e){
    const btn=e.target.closest('.pill');
    if(!btn)return;
    filterBtns.querySelectorAll('.pill').forEach(function(b){b.classList.remove('active');b.setAttribute('aria-pressed','false');});
    btn.classList.add('active');
    btn.setAttribute('aria-pressed','true');
    activeFilter=btn.dataset.filter;
    renderLimit=20;render();
    syncHash();
});

searchInput.addEventListener('input',function(){
    clearTimeout(debounceTimer);
    clearBtn.style.display=searchInput.value?'block':'none';
    debounceTimer=setTimeout(function(){searchText=searchInput.value.trim();renderLimit=20;render();syncHash();},150);
});
clearBtn.addEventListener('click',function(){searchInput.value='';clearBtn.style.display='none';searchText='';renderLimit=20;render();syncHash();});

// Feature 2: Expand/Collapse All
document.getElementById('expandAll').addEventListener('click',function(){
    container.querySelectorAll('.grp').forEach(function(g){g.classList.add('open');});
    container.querySelectorAll('.t-detail').forEach(function(d){d.classList.add('open');});
    document.querySelectorAll('.qa-section').forEach(function(s){s.classList.add('tl-open');});
});
document.getElementById('collapseAll').addEventListener('click',function(){
    container.querySelectorAll('.grp').forEach(function(g){g.classList.remove('open');});
    container.querySelectorAll('.t-detail').forEach(function(d){d.classList.remove('open');});
    document.querySelectorAll('.qa-section').forEach(function(s){s.classList.remove('tl-open');});
});

// Feature 3: Sort Toggle
document.querySelectorAll('.sort-group .sort-btn').forEach(function(btn){
    btn.addEventListener('click',function(){
        document.querySelectorAll('.sort-group .sort-btn').forEach(function(b){b.classList.remove('active');b.setAttribute('aria-checked','false');});
        btn.classList.add('active');
        btn.setAttribute('aria-checked','true');
        sortMode = btn.dataset.sort;
        renderLimit=20;render();
        syncHash();
    });
});

// Group-By Toggle
document.querySelectorAll('.grp-toggle .sort-btn').forEach(function(btn){
    btn.addEventListener('click',function(){
        document.querySelectorAll('.grp-toggle .sort-btn').forEach(function(b){b.classList.remove('active');b.setAttribute('aria-checked','false');});
        btn.classList.add('active');
        btn.setAttribute('aria-checked','true');
        groupMode = btn.dataset.group;
        renderLimit=20;render();
        syncHash();
    });
});

// Quick-access section click delegation
document.addEventListener('click',function(e){
    const qi = e.target.closest('.qa-item');
    if(qi && qi.dataset.scrollTid){scrollToTest(qi.dataset.scrollTid);return;}
    const cb = e.target.closest('.copy-btn');
    if(cb){
        const wrap = cb.closest('.d-pre-wrap');
        const pre = wrap && wrap.querySelector('.d-pre');
        if(pre){
            navigator.clipboard.writeText(pre.textContent).then(function(){
                cb.innerHTML = checkIcon;
                cb.classList.add('copied');
                setTimeout(function(){cb.innerHTML=copyIcon;cb.classList.remove('copied');},1500);
            });
        }
    }
});

// Theme initialization
const savedTheme = localStorage.getItem('tunit-theme');
const prefersDark = window.matchMedia('(prefers-color-scheme:dark)').matches;
const initTheme = savedTheme || (prefersDark ? 'dark' : 'light');
document.documentElement.setAttribute('data-theme', initTheme);

// Render global execution timeline (static, doesn't change with filters)
document.getElementById('globalTimeline').innerHTML = renderGlobalTimeline();

loadFromHash();
render();
renderFailedSection();
renderSlowestSection();
checkHash();

// Theme toggle handler
document.getElementById('themeToggle').addEventListener('click', function(){
    document.body.classList.add('theme-transition');
    const current = document.documentElement.getAttribute('data-theme');
    const next = current === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', next);
    localStorage.setItem('tunit-theme', next);
    setTimeout(function(){document.body.classList.remove('theme-transition');}, 350);
});

// ── Feature 7: Keyboard Navigation ──────────────────
function getVisibleRows(){return Array.from(container.querySelectorAll('.t-row'));}
function setKbFocus(idx){
    const rows = getVisibleRows();
    const old = container.querySelector('.t-row.kb-focus');
    if(old) old.classList.remove('kb-focus');
    if(idx<0||idx>=rows.length){kbIdx=-1;return;}
    kbIdx=idx;
    const row=rows[idx];
    row.classList.add('kb-focus');
    const grp=row.closest('.grp');
    if(grp&&!grp.classList.contains('open')) grp.classList.add('open');
    row.scrollIntoView({behavior:'smooth',block:'nearest'});
}
function showKbHelp(){
    let ov=document.getElementById('kbOverlay');
    if(ov){ov.remove();return;}
    ov=document.createElement('div');ov.id='kbOverlay';ov.className='kb-overlay';
    ov.setAttribute('role','dialog');ov.setAttribute('aria-modal','true');ov.setAttribute('aria-label','Keyboard shortcuts');
    ov.innerHTML='<div class="kb-modal"><h3>Keyboard Shortcuts</h3>'
        +'<div class="kb-row"><span>Next test</span><span class="kb-key">j</span></div>'
        +'<div class="kb-row"><span>Previous test</span><span class="kb-key">k</span></div>'
        +'<div class="kb-row"><span>Toggle detail</span><span class="kb-key">Enter</span></div>'
        +'<div class="kb-row"><span>Close / clear</span><span class="kb-key">Esc</span></div>'
        +'<div class="kb-row"><span>Focus search</span><span class="kb-key">/</span></div>'
        +'<div class="kb-row"><span>This help</span><span class="kb-key">?</span></div>'
        +'<button class="bar-btn" style="margin-top:14px" aria-label="Close" id="kbClose">&times;</button>'
        +'</div>';
    ov.addEventListener('click',function(ev){if(ev.target===ov)ov.remove();});
    document.body.appendChild(ov);
    document.getElementById('kbClose').focus();
    document.getElementById('kbClose').addEventListener('click',function(){ov.remove();});
}
document.addEventListener('keydown',function(e){
    const tag=e.target.tagName;
    if(tag==='INPUT'||tag==='TEXTAREA'||tag==='SELECT'){
        if(e.key==='Escape'){e.target.blur();if(e.target===searchInput){searchInput.value='';clearBtn.style.display='none';searchText='';renderLimit=20;render();syncHash();}}
        return;
    }
    const ov=document.getElementById('kbOverlay');
    if(ov&&e.key==='Escape'){ov.remove();return;}
    if(e.key==='j'){e.preventDefault();const rows=getVisibleRows();setKbFocus(Math.min(kbIdx+1,rows.length-1));return;}
    if(e.key==='k'){e.preventDefault();setKbFocus(Math.max(kbIdx-1,0));return;}
    if(e.key==='Enter'&&kbIdx>=0){
        e.preventDefault();const rows=getVisibleRows();const row=rows[kbIdx];
        if(row){const det=container.querySelector('.t-detail[data-gi="'+row.dataset.gi+'"][data-ti="'+row.dataset.ti+'"]');if(det)det.classList.toggle('open');}
        return;
    }
    if(e.key==='Escape'){
        const focused=container.querySelector('.t-row.kb-focus');
        if(focused)focused.classList.remove('kb-focus');
        kbIdx=-1;
        container.querySelectorAll('.t-detail.open').forEach(function(d){d.classList.remove('open');});
        return;
    }
    if(e.key==='/'){e.preventDefault();searchInput.focus();return;}
    if(e.key==='?'){e.preventDefault();showKbHelp();return;}
});

// ── Feature 8: Sticky Mini-Header ───────────────────
(function(){
    const dash=document.querySelector('.dash');
    const bar=document.getElementById('stickyBar');
    if(!dash||!bar)return;
    const obs=new IntersectionObserver(function(entries){
        entries.forEach(function(en){bar.classList.toggle('visible',!en.isIntersecting);});
    },{threshold:0});
    obs.observe(dash);
    var ssb=document.getElementById('stickySearchBtn');
    if(ssb) ssb.addEventListener('click',function(){searchInput.focus();searchInput.scrollIntoView({behavior:'smooth',block:'center'});});
})();

// ── Feature 9: 100% Pass Celebration ────────────────
if(data.summary.passed===data.summary.total&&data.summary.total>0){
    const dash=document.querySelector('.dash');
    if(dash) dash.classList.add('celebrate');
}

// ── Feature 10: Duration Histogram ──────────────────
(function(){
    const hist=document.getElementById('durationHist');
    if(!hist)return;
    const durations=[];
    groups.forEach(function(g){g.tests.forEach(function(t){durations.push(t.durationMs);});});
    if(!durations.length)return;
    const mn=Math.min.apply(null,durations);
    const mx=Math.max.apply(null,durations);
    if(mx<=mn){hist.innerHTML='<div class="dur-hist-bar" style="height:100%" data-tip="'+durations.length+' tests at '+fmt(mn)+'"></div>';return;}
    const bins=10;const step=(mx-mn)/bins;const buckets=new Array(bins).fill(0);
    durations.forEach(function(d){var i=Math.min(Math.floor((d-mn)/step),bins-1);buckets[i]++;});
    const maxB=Math.max.apply(null,buckets)||1;
    let h='';
    for(var i=0;i<bins;i++){
        const lo=mn+i*step;const hi=lo+step;
        const pct=Math.max((buckets[i]/maxB)*100,2);
        h+='<div class="dur-hist-bar" style="height:'+pct.toFixed(1)+'%" data-tip="'+fmt(lo)+' \u2013 '+fmt(hi)+': '+buckets[i]+'"></div>';
    }
    hist.innerHTML=h;
})();
})();
""";
    }
}
