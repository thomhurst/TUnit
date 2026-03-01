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
        sb.AppendLine("<html lang=\"en\">");

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

        // Ambient background grain
        sb.AppendLine("<div class=\"grain\"></div>");

        sb.AppendLine("<div class=\"shell\">");

        AppendHeader(sb, data);
        AppendSummaryDashboard(sb, data.Summary, data.TotalDurationMs);
        AppendSearchAndFilters(sb);
        AppendTestGroups(sb, data);
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
        AppendMetaChip(sb, "tag", "TUnit " + data.TUnitVersion);
        if (!string.IsNullOrEmpty(data.Filter))
        {
            AppendMetaChip(sb, "filter", data.Filter!);
        }

        sb.AppendLine("</div>");
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

    private static void AppendSummaryDashboard(StringBuilder sb, ReportSummary summary, double totalDurationMs)
    {
        var passRate = summary.Total > 0 ? (double)summary.Passed / summary.Total * 100 : 0;

        sb.AppendLine("<section class=\"dash\" data-anim=\"fade-up\">");

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

    private static void AppendSearchAndFilters(StringBuilder sb)
    {
        sb.AppendLine("<div class=\"bar\" data-anim=\"fade-up\">");
        sb.AppendLine("<div class=\"search\">");
        // Search icon inline SVG
        sb.AppendLine("<svg class=\"search-icon\" viewBox=\"0 0 20 20\" fill=\"currentColor\"><path fill-rule=\"evenodd\" d=\"M9 3.5a5.5 5.5 0 1 0 0 11 5.5 5.5 0 0 0 0-11ZM2 9a7 7 0 1 1 12.45 4.39l3.58 3.58a.75.75 0 1 1-1.06 1.06l-3.58-3.58A7 7 0 0 1 2 9Z\" clip-rule=\"evenodd\"/></svg>");
        sb.AppendLine("<input type=\"text\" id=\"searchInput\" placeholder=\"Search tests\u2026\" autocomplete=\"off\" spellcheck=\"false\">");
        sb.AppendLine("<button id=\"clearSearch\" class=\"search-clear\" aria-label=\"Clear\">&times;</button>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"pills\" id=\"filterButtons\">");
        sb.AppendLine("<button class=\"pill active\" data-filter=\"all\">All</button>");
        sb.AppendLine("<button class=\"pill\" data-filter=\"passed\"><span class=\"dot emerald\"></span>Passed</button>");
        sb.AppendLine("<button class=\"pill\" data-filter=\"failed\"><span class=\"dot rose\"></span>Failed</button>");
        sb.AppendLine("<button class=\"pill\" data-filter=\"skipped\"><span class=\"dot amber\"></span>Skipped</button>");
        sb.AppendLine("<button class=\"pill\" data-filter=\"cancelled\"><span class=\"dot slate\"></span>Cancelled</button>");
        sb.AppendLine("</div>");
        sb.AppendLine("<span class=\"bar-info\" id=\"filterSummary\"></span>");
        sb.AppendLine("</div>");
    }

    private static void AppendTestGroups(StringBuilder sb, ReportData data)
    {
        if (data.Summary.Total == 0)
        {
            sb.AppendLine("<div class=\"empty\">No tests were discovered.</div>");
            return;
        }

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

        if (ms < 1000)
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

/* ── Header ────────────────────────────────────────── */
.hdr{
  display:flex;align-items:flex-start;justify-content:space-between;
  flex-wrap:wrap;gap:16px;margin-bottom:28px;
  animation:fade-up .5s var(--ease) both;
}
.hdr-brand{display:flex;align-items:center;gap:14px}
.hdr-logo{width:38px;height:38px;flex-shrink:0}
.hdr-name{
  font-size:1.35rem;font-weight:700;letter-spacing:-.02em;
  background:linear-gradient(135deg,#e2e4e9 30%,#818cf8);
  -webkit-background-clip:text;-webkit-text-fill-color:transparent;
  background-clip:text;
}
.hdr-sub{font-size:.78rem;color:var(--text-3);letter-spacing:.06em;text-transform:uppercase}
.hdr-meta{display:flex;gap:8px;flex-wrap:wrap;align-items:center}
.chip{
  display:inline-flex;align-items:center;gap:6px;
  padding:5px 12px;border-radius:100px;
  background:var(--surface-1);border:1px solid var(--border);
  font-size:.78rem;color:var(--text-2);white-space:nowrap;
}

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
.stat-n{display:block;font-size:1.65rem;font-weight:800;letter-spacing:-.03em;line-height:1.1}
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
.bar{display:flex;align-items:center;gap:12px;flex-wrap:wrap;margin-bottom:20px}
.search{
  position:relative;flex:1;min-width:220px;max-width:380px;
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
  display:flex;align-items:center;gap:12px;padding:11px 16px;
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
.grp-body{display:none;border-top:1px solid var(--border)}
.grp.open .grp-body{display:block}

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
.t-badge.passed{background:var(--emerald-d);color:var(--emerald)}
.t-badge.failed,.t-badge.error,.t-badge.timedOut{background:var(--rose-d);color:var(--rose)}
.t-badge.skipped{background:var(--amber-d);color:var(--amber)}
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
  display:none;padding:14px 18px 14px 22px;
  background:var(--surface-1);border-bottom:1px solid var(--border);
}
.t-detail.open{display:block;animation:fade-up .25s var(--ease)}
.d-sec{margin-bottom:14px}
.d-sec:last-child{margin-bottom:0}
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
}
.d-pre.err{color:var(--rose);border-color:rgba(251,113,133,.15)}
.d-pre.stack{color:var(--text-3);font-size:.76rem}
.d-tags{display:flex;gap:6px;flex-wrap:wrap}
.d-tag{
  padding:3px 10px;border-radius:100px;font-size:.76rem;
  background:var(--surface-2);color:var(--text-2);border:1px solid var(--border);
}
.d-tag.kv{font-family:var(--mono);font-size:.72rem}
.timing-bars{display:flex;flex-direction:column;gap:5px}
.tb{display:flex;align-items:center;gap:8px;font-size:.8rem}
.tb-name{min-width:80px;color:var(--text-3);font-size:.76rem}
.tb-track{flex:1;height:6px;border-radius:3px;background:var(--surface-2);overflow:hidden}
.tb-fill{height:100%;border-radius:3px;background:linear-gradient(90deg,var(--indigo),var(--emerald));min-width:2px;transition:width .4s var(--ease)}
.tb-val{font-family:var(--mono);color:var(--text-3);font-size:.76rem;min-width:52px;text-align:right}
.d-src{font-size:.78rem;color:var(--text-3);font-family:var(--mono)}

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
}

/* ── Print ─────────────────────────────────────────── */
@media print{
  body{background:#fff;color:#000}
  .grain,.bar{display:none}
  .grp-body{display:block!important}
  .t-detail{display:block!important;background:#f9f9f9}
  .shell{max-width:none;padding:0}
}

/* ── Scrollbar ─────────────────────────────────────── */
::-webkit-scrollbar{width:6px;height:6px}
::-webkit-scrollbar-track{background:transparent}
::-webkit-scrollbar-thumb{background:var(--surface-3);border-radius:3px}
::-webkit-scrollbar-thumb:hover{background:var(--text-3)}
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

const spansByTrace = {};
spans.forEach(s => {
    if (!spansByTrace[s.traceId]) spansByTrace[s.traceId] = [];
    spansByTrace[s.traceId].push(s);
});

function matchesFilter(t) {
    if (activeFilter !== 'all') {
        if (activeFilter === 'failed') {
            if (t.status !== 'failed' && t.status !== 'error' && t.status !== 'timedOut') return false;
        } else if (t.status !== activeFilter) return false;
    }
    if (searchText) {
        const q = searchText.toLowerCase();
        const h = (t.displayName + ' ' + t.className + ' ' + (t.categories||[]).join(' ')).toLowerCase();
        if (!h.includes(q)) return false;
    }
    return true;
}

function fmt(ms) {
    if (ms < 1) return '<1ms';
    if (ms < 1000) return ms.toFixed(0) + 'ms';
    if (ms < 60000) return (ms/1000).toFixed(2) + 's';
    return (ms/60000).toFixed(1) + 'm';
}

function esc(s) {
    if (!s) return '';
    const d = document.createElement('div');
    d.textContent = s;
    return d.innerHTML;
}

const arrow = '<svg class="grp-arrow" viewBox="0 0 16 16" fill="currentColor"><path d="M6.22 4.22a.75.75 0 0 1 1.06 0l3.25 3.25a.75.75 0 0 1 0 1.06l-3.25 3.25a.75.75 0 0 1-1.06-1.06L8.94 8 6.22 5.28a.75.75 0 0 1 0-1.06Z"/></svg>';

function renderDetail(t) {
    let h = '';
    if (t.exception) {
        h += '<div class="d-sec"><div class="d-lbl">Exception</div>';
        h += '<div class="d-pre err">' + esc(t.exception.type) + ': ' + esc(t.exception.message) + '</div>';
        if (t.exception.stackTrace) h += '<div class="d-pre stack">' + esc(t.exception.stackTrace) + '</div>';
        let inner = t.exception.innerException;
        while (inner) {
            h += '<div class="d-lbl" style="margin-top:8px">Inner Exception</div>';
            h += '<div class="d-pre err">' + esc(inner.type) + ': ' + esc(inner.message) + '</div>';
            if (inner.stackTrace) h += '<div class="d-pre stack">' + esc(inner.stackTrace) + '</div>';
            inner = inner.innerException;
        }
        h += '</div>';
    }
    if (t.output) {
        h += '<div class="d-sec"><div class="d-lbl">Standard Output</div>';
        h += '<div class="d-pre">' + esc(t.output) + '</div></div>';
    }
    if (t.errorOutput) {
        h += '<div class="d-sec"><div class="d-lbl">Error Output</div>';
        h += '<div class="d-pre err">' + esc(t.errorOutput) + '</div></div>';
    }
    if (t.skipReason) {
        h += '<div class="d-sec"><div class="d-lbl">Skip Reason</div>';
        h += '<div class="d-pre">' + esc(t.skipReason) + '</div></div>';
    }
    if (t.timingSteps && t.timingSteps.length > 0) {
        const mx = Math.max(...t.timingSteps.map(s=>s.durationMs),1);
        h += '<div class="d-sec"><div class="d-lbl">Timing</div><div class="timing-bars">';
        t.timingSteps.forEach(s => {
            const pct = Math.max((s.durationMs/mx)*100,1);
            h += '<div class="tb"><span class="tb-name">'+esc(s.name)+'</span>';
            h += '<div class="tb-track"><div class="tb-fill" style="width:'+pct.toFixed(1)+'%"></div></div>';
            h += '<span class="tb-val">'+fmt(s.durationMs)+'</span></div>';
        });
        h += '</div></div>';
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
    if (t.retryAttempt > 0) {
        h += '<div class="d-sec"><span class="retry-tag">Retry attempt '+t.retryAttempt+'</span></div>';
    }
    if (t.traceId && t.spanId && spansByTrace[t.traceId]) h += renderTrace(t.traceId, t.spanId);
    return h;
}

function renderTrace(tid, rootSpanId) {
    const allSpans = spansByTrace[tid];
    if (!allSpans || !allSpans.length) return '';
    // Build lookup and filter to only the test's own span + its descendants
    const byId={};
    allSpans.forEach(s=>{byId[s.spanId]=s});
    const included=new Set();
    function includeDescendants(sid){
        if(included.has(sid))return;
        included.add(sid);
        allSpans.forEach(s=>{if(s.parentSpanId===sid)includeDescendants(s.spanId)});
    }
    includeDescendants(rootSpanId);
    const sp=allSpans.filter(s=>included.has(s.spanId));
    if (!sp.length) return '';
    const mn = Math.min(...sp.map(s=>s.startTimeMs));
    const mx = Math.max(...sp.map(s=>s.startTimeMs+s.durationMs));
    const dur = mx-mn||1;
    const depth={};
    function gd(s){
        if(depth[s.spanId]!==undefined)return depth[s.spanId];
        if(!s.parentSpanId||!byId[s.parentSpanId]||!included.has(s.parentSpanId)){depth[s.spanId]=0;return 0}
        depth[s.spanId]=gd(byId[s.parentSpanId])+1;return depth[s.spanId];
    }
    sp.forEach(gd);
    const sorted=[...sp].sort((a,b)=>a.startTimeMs-b.startTimeMs);
    let h='<div class="d-sec"><div class="d-lbl">Trace Timeline</div><div class="trace">';
    sorted.forEach((s,i)=>{
        const d=depth[s.spanId]||0;
        const l=((s.startTimeMs-mn)/dur*100).toFixed(2);
        const w=Math.max((s.durationMs/dur*100),.5).toFixed(2);
        const cls=s.status==='Error'?'err':s.status==='Ok'?'ok':'unk';
        h+='<div class="sp-row" data-si="'+i+'">';
        h+='<span class="sp-indent" style="width:'+(d*14)+'px"></span>';
        h+='<div class="sp-bar '+cls+'" style="margin-left:'+l+'%;width:'+w+'%" title="'+esc(s.name)+' ('+fmt(s.durationMs)+')"></div>';
        h+='<span class="sp-name">'+esc(s.name)+'</span>';
        h+='<span class="sp-dur">'+fmt(s.durationMs)+'</span>';
        h+='</div>';
        let ex='<div class="sp-extra" id="sp-'+tid+'-'+i+'">';
        ex+='<strong>Source:</strong> '+esc(s.source)+' &middot; <strong>Kind:</strong> '+esc(s.kind);
        if(s.tags&&s.tags.length){ex+='<br><strong>Tags:</strong> ';s.tags.forEach(t=>{ex+=esc(t.key)+'='+esc(t.value)+' '});}
        if(s.events&&s.events.length){ex+='<br><strong>Events:</strong> ';s.events.forEach(e=>{ex+=esc(e.name)+' ';if(e.tags)e.tags.forEach(t=>{ex+=esc(t.key)+'='+esc(t.value)+' '})});}
        ex+='</div>';
        h+=ex;
    });
    h+='</div></div>';
    return h;
}

function render() {
    let total = 0;
    let html = '';
    groups.forEach((g,gi)=>{
        const ft = g.tests.filter(matchesFilter);
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
        html += '<div class="grp-hd'+(fail?' fail':'')+'">';
        html += '<div class="grp-indicator"></div>';
        html += arrow;
        html += '<span class="grp-name">'+esc(g.className)+'</span>';
        html += '<span class="grp-badges">';
        if(c.p) html += '<span class="grp-b gp">'+c.p+'</span>';
        if(c.f) html += '<span class="grp-b gf">'+c.f+'</span>';
        if(c.s) html += '<span class="grp-b gs">'+c.s+'</span>';
        html += '<span class="grp-b gt">'+ft.length+'</span>';
        html += '</span></div>';
        html += '<div class="grp-body">';
        ft.forEach((t,ti)=>{
            html += '<div class="t-row" data-gi="'+gi+'" data-ti="'+ti+'">';
            html += '<span class="t-badge '+t.status+'">'+esc(t.status)+'</span>';
            html += '<span class="t-name">'+esc(t.displayName)+'</span>';
            if(t.retryAttempt>0) html += '<span class="retry-tag">retry '+t.retryAttempt+'</span>';
            html += '<span class="t-dur">'+fmt(t.durationMs)+'</span>';
            html += '</div>';
            html += '<div class="t-detail" data-gi="'+gi+'" data-ti="'+ti+'">';
            html += renderDetail(t);
            html += '</div>';
        });
        html += '</div></div>';
    });
    container.innerHTML = html;
    filterSummary.textContent = (activeFilter!=='all'||searchText)
        ? 'Showing '+total+' of '+data.summary.total+' tests' : '';
}

container.addEventListener('click',function(e){
    const hd = e.target.closest('.grp-hd');
    if(hd){hd.parentElement.classList.toggle('open');return;}
    const row = e.target.closest('.t-row');
    if(row){
        const det = container.querySelector('.t-detail[data-gi="'+row.dataset.gi+'"][data-ti="'+row.dataset.ti+'"]');
        if(det) det.classList.toggle('open');
        return;
    }
    const sr = e.target.closest('.sp-row');
    if(sr){const nx=sr.nextElementSibling;if(nx&&nx.classList.contains('sp-extra'))nx.classList.toggle('open');}
});

filterBtns.addEventListener('click',function(e){
    const btn=e.target.closest('.pill');
    if(!btn)return;
    filterBtns.querySelectorAll('.pill').forEach(b=>b.classList.remove('active'));
    btn.classList.add('active');
    activeFilter=btn.dataset.filter;
    render();
});

searchInput.addEventListener('input',function(){
    clearTimeout(debounceTimer);
    clearBtn.style.display=searchInput.value?'block':'none';
    debounceTimer=setTimeout(function(){searchText=searchInput.value.trim();render();},150);
});
clearBtn.addEventListener('click',function(){searchInput.value='';clearBtn.style.display='none';searchText='';render();});

render();
})();
""";
    }
}
