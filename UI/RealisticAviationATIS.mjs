/*!
 * Cities: Skylines II UI Module
 *
 * Id: RealisticAviationATIS
 * Author: Boing787(kanghyeon01)
 * Version: 0.1.0
 * Dependencies:
 */
// Hand-written (no bundler): uses only the globals the game exposes to UI mods.
const React = window.React;
const modding = window["cs2/modding"];
const l10n = window["cs2/l10n"];
const ui = window["cs2/ui"];

const SECTION_TYPE = "RealisticAviationATIS.AtisSection";
const SECTIONS_MODULE = "game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx";

// The report as a station prints it: monospace on a dark panel, like the radio boxes of Realistic Aviation.
const REPORT_STYLE = {
    backgroundColor: "rgba(0, 0, 0, 0.35)",
    border: "1rem solid rgba(255, 255, 255, 0.2)",
    borderRadius: "4rem",
    padding: "8rem 10rem",
    fontSize: "13rem",
    lineHeight: 1.35,
    fontFamily: "monospace",
    color: "rgb(130, 200, 255)",
};
const EMPTY_STYLE = { color: "rgba(255, 255, 255, 0.5)", fontFamily: "inherit" };
const DECODED_STYLE = { fontSize: "13rem", lineHeight: 1.4 };
// The ATIS recording: plain text, as it is read out.
const ATIS_STYLE = Object.assign({}, REPORT_STYLE, { fontFamily: "inherit", color: "white" });

function getGameModule(path, name) {
    try {
        return modding.getModule(path, name);
    } catch (e) {
        return undefined;
    }
}

const InfoSection = getGameModule("game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.tsx", "InfoSection");
const InfoRow = getGameModule("game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.tsx", "InfoRow");
const Tooltip = ui && ui.Tooltip;

function decodedLines(decoded) {
    return React.createElement("div", { style: DECODED_STYLE },
        ...decoded.map((line, i) => React.createElement("div", { key: i }, line)));
}

// Airport info panel: the latest METAR or SPECI, in plain words on hover.
function AtisSection(props) {
    const { translate } = l10n.useLocalization();
    const decoded = props.decoded || [];
    const rows = [React.createElement(InfoRow, { key: "title", left: translate("RealisticAviationATIS.Section.Label", "Weather report"), uppercase: true })];
    if (!props.metar) {
        rows.push(React.createElement("div", { key: "report", style: Object.assign({}, REPORT_STYLE, EMPTY_STYLE) },
            translate("RealisticAviationATIS.Section.Empty", "No report yet")));
    } else if (Tooltip) {
        rows.push(React.createElement(Tooltip, { key: "report", tooltip: decodedLines(decoded) },
            React.createElement("div", { style: REPORT_STYLE }, props.metar)));
    } else {
        // Without the game's tooltip component, the plain words go under the report.
        rows.push(React.createElement("div", { key: "report", style: REPORT_STYLE }, props.metar));
        rows.push(React.createElement("div", { key: "decoded", style: { marginTop: "6rem" } }, decodedLines(decoded)));
    }
    // ATIS: "Bravo 128.475" on the title row, the recording below, its plain words on hover (as the report).
    rows.push(React.createElement(InfoRow, {
        key: "atisTitle",
        left: translate("RealisticAviationATIS.Atis.Label", "ATIS"),
        right: props.atisLetter ? props.atisLetter + " " + props.atisFrequency : "",
        uppercase: true,
    }));
    const atisDecoded = props.atisDecoded || [];
    if (!props.atisText) {
        rows.push(React.createElement("div", { key: "atis", style: Object.assign({}, ATIS_STYLE, EMPTY_STYLE) },
            translate("RealisticAviationATIS.Atis.Empty", "No broadcast yet")));
    } else if (Tooltip) {
        rows.push(React.createElement(Tooltip, { key: "atis", tooltip: decodedLines(atisDecoded) },
            React.createElement("div", { style: ATIS_STYLE }, props.atisText)));
    } else {
        rows.push(React.createElement("div", { key: "atis", style: ATIS_STYLE }, props.atisText));
        rows.push(React.createElement("div", { key: "atisDecoded", style: { marginTop: "6rem" } }, decodedLines(atisDecoded)));
    }
    return React.createElement(InfoSection, { disableFocus: true }, ...rows);
}

export default function register(moduleRegistry) {
    if (!InfoSection || !InfoRow) {
        console.error("RealisticAviationATIS: info panel components not found; the weather report is unavailable.");
        return;
    }
    moduleRegistry.extend(SECTIONS_MODULE, "selectedInfoSectionComponents", (components) => {
        components[SECTION_TYPE] = AtisSection;
        return components;
    });
}

export const hasCSS = false;
