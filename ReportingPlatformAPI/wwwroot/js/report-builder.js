(() => {
    const state = {
        entities: [],
        fields: [],
        currentBaseFields: []
    };

    const selectors = {
        baseEntity: document.getElementById("baseEntitySelect"),
        selectFields: document.getElementById("selectFieldsSelect"),
        manualSelectFields: document.getElementById("manualSelectFieldsInput"),
        groupBy: document.getElementById("groupBySelect"),
        metricKey: document.getElementById("metricKeyInput"),
        metricFunction: document.getElementById("metricFunctionSelect"),
        metricField: document.getElementById("metricFieldSelect"),
        metricAlias: document.getElementById("metricAliasInput"),
        filterField: document.getElementById("filterFieldSelect"),
        filterOperator: document.getElementById("filterOperatorSelect"),
        filterValue: document.getElementById("filterValueInput"),
        sortField: document.getElementById("sortFieldSelect"),
        sortDirection: document.getElementById("sortDirectionSelect"),
        limit: document.getElementById("limitInput"),
        errors: document.getElementById("errorPanel"),
        sql: document.getElementById("sqlPreview"),
        parameters: document.getElementById("parametersPreview"),
        resultSummary: document.getElementById("resultSummary"),
        grid: document.getElementById("resultsGrid"),
        fallback: document.getElementById("resultsTableFallback")
    };

    document.addEventListener("DOMContentLoaded", init);

    async function init() {
        wireEvents();
        await loadEntities();
        await loadAllFields();
        populateFieldControls();
        await setBaseEntity("patient");
    }

    function wireEvents() {
        selectors.baseEntity.addEventListener("change", async () => {
            await setBaseEntity(selectors.baseEntity.value);
        });
        selectors.selectFields.addEventListener("change", () => {
            syncGroupByOptions();
            syncSortOptions();
        });
        selectors.metricAlias.addEventListener("input", syncSortOptions);
        selectors.metricFunction.addEventListener("change", syncSortOptions);
        document.getElementById("loadDemoButton").addEventListener("click", loadDemoQuery);
        document.getElementById("runReportButton").addEventListener("click", runReport);
    }

    async function loadEntities() {
        state.entities = await fetchJson("/api/report-metadata/entities");
        selectors.baseEntity.innerHTML = state.entities
            .map(entity => `<option value="${escapeHtml(entity.entityKey)}">${escapeHtml(entity.displayName)}</option>`)
            .join("");
    }

    async function loadAllFields() {
        const all = [];
        for (const entity of state.entities) {
            const fields = await fetchJson(`/api/report-metadata/entities/${encodeURIComponent(entity.entityKey)}/fields`);
            all.push(...fields);
        }

        state.fields = all;
    }

    async function setBaseEntity(entityKey) {
        selectors.baseEntity.value = entityKey;
        state.currentBaseFields = await fetchJson(`/api/report-metadata/entities/${encodeURIComponent(entityKey)}/fields`);
        populateFieldControls();
        syncGroupByOptions();
        syncSortOptions();
    }

    function populateFieldControls() {
        const options = state.fields.map(field => fieldOption(field)).join("");
        selectors.selectFields.innerHTML = options;
        selectors.metricField.innerHTML = `<option value="">None</option>${options}`;
        selectors.filterField.innerHTML = `<option value="">None</option>${options}`;
    }

    function fieldOption(field) {
        const label = `${field.displayName} (${field.semanticKey})`;
        return `<option value="${escapeHtml(field.semanticKey)}" data-groupable="${field.isGroupable}">${escapeHtml(label)}</option>`;
    }

    function syncGroupByOptions() {
        const selected = selectedValues(selectors.selectFields);
        const groupable = state.fields.filter(field => selected.includes(field.semanticKey) && field.isGroupable);
        const previouslySelected = new Set(selectedValues(selectors.groupBy));
        selectors.groupBy.innerHTML = groupable.map(field => {
            const selectedAttr = previouslySelected.has(field.semanticKey) ? " selected" : "";
            return `<option value="${escapeHtml(field.semanticKey)}"${selectedAttr}>${escapeHtml(field.displayName)} (${escapeHtml(field.semanticKey)})</option>`;
        }).join("");
    }

    function syncSortOptions() {
        const fields = selectedValues(selectors.selectFields);
        const metricAlias = selectors.metricAlias.value.trim();
        const hasMetric = selectors.metricFunction.value && selectors.metricField.value && metricAlias;
        const options = ['<option value="">None</option>'];

        for (const semanticKey of fields) {
            const field = state.fields.find(item => item.semanticKey === semanticKey);
            if (field) {
                options.push(`<option value="${escapeHtml(semanticKey)}">${escapeHtml(field.displayName)} (${escapeHtml(semanticKey)})</option>`);
            }
        }

        if (hasMetric) {
            options.push(`<option value="${escapeHtml(metricAlias)}">${escapeHtml(metricAlias)} metric</option>`);
        }

        const current = selectors.sortField.value;
        selectors.sortField.innerHTML = options.join("");
        if ([...selectors.sortField.options].some(option => option.value === current)) {
            selectors.sortField.value = current;
        }
    }

    function loadDemoQuery() {
        clearError();
        selectors.baseEntity.value = "patient";
        setSelections(selectors.selectFields, [
            "clinical_study.study_code",
            "study_site.country",
            "patient.gender"
        ]);
        selectors.metricKey.value = "patient_count";
        selectors.manualSelectFields.value = "";
        selectors.metricFunction.value = "CountDistinct";
        selectors.metricField.value = "patient.patient_id";
        selectors.metricAlias.value = "PatientCount";
        selectors.filterField.value = "patient.gender";
        selectors.filterOperator.value = "Equals";
        selectors.filterValue.value = "Male";
        syncGroupByOptions();
        setSelections(selectors.groupBy, [
            "clinical_study.study_code",
            "study_site.country",
            "patient.gender"
        ]);
        syncSortOptions();
        selectors.sortField.value = "PatientCount";
        selectors.sortDirection.value = "Desc";
        selectors.limit.value = "100";
    }

    async function runReport() {
        clearError();
        selectors.resultSummary.textContent = "Running...";

        try {
            const response = await fetch("/api/report-query/execute", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(buildRequest())
            });
            const payload = await response.json();

            if (!response.ok || payload.success === false) {
                showError(payload.errors || payload.error || "Report query failed.");
                selectors.resultSummary.textContent = "No rows";
                return;
            }

            renderResult(payload);
        } catch (error) {
            showError(error.message || "Unable to run report.");
            selectors.resultSummary.textContent = "No rows";
        }
    }

    function buildRequest() {
        const metrics = [];
        if (selectors.metricFunction.value && selectors.metricField.value && selectors.metricAlias.value.trim()) {
            metrics.push({
                metricKey: selectors.metricKey.value.trim() || selectors.metricAlias.value.trim().toLowerCase(),
                function: selectors.metricFunction.value,
                field: selectors.metricField.value,
                alias: selectors.metricAlias.value.trim()
            });
        }

        const filters = [];
        if (selectors.filterField.value && selectors.filterOperator.value) {
            filters.push({
                field: selectors.filterField.value,
                operator: selectors.filterOperator.value,
                value: parseFilterValue(selectors.filterOperator.value, selectors.filterValue.value)
            });
        }

        const sort = [];
        if (selectors.sortField.value) {
            sort.push({
                field: selectors.sortField.value,
                direction: selectors.sortDirection.value
            });
        }

        return {
            baseEntity: selectors.baseEntity.value,
            selectFields: [...new Set([...selectedValues(selectors.selectFields), ...manualFields(selectors.manualSelectFields.value)])],
            metrics,
            filters,
            groupBy: selectedValues(selectors.groupBy),
            sort,
            limit: Number(selectors.limit.value || 100)
        };
    }

    function parseFilterValue(operator, value) {
        if (operator === "IsNull" || operator === "IsNotNull") {
            return null;
        }

        if (operator === "Between" || operator === "In") {
            return value.split(",").map(item => item.trim()).filter(Boolean);
        }

        return value;
    }

    function manualFields(value) {
        return value
            .split(/[,\n]/)
            .map(item => item.trim())
            .filter(Boolean);
    }

    function renderResult(result) {
        selectors.sql.textContent = result.generatedSql || "";
        selectors.parameters.textContent = JSON.stringify(result.parameters || {}, null, 2);
        selectors.resultSummary.textContent = `${result.rowCount} rows in ${result.executionTimeMs} ms`;

        if (window.jQuery && window.kendo && jQuery.fn.kendoGrid) {
            selectors.fallback.innerHTML = "";
            const grid = jQuery(selectors.grid).data("kendoGrid");
            if (grid) {
                grid.destroy();
                selectors.grid.innerHTML = "";
            }

            jQuery(selectors.grid).kendoGrid({
                dataSource: {
                    data: result.rows || [],
                    pageSize: 20
                },
                pageable: true,
                sortable: true,
                resizable: true,
                columnMenu: true,
                columns: (result.columns || []).map(column => ({
                    field: column.name,
                    title: column.name
                }))
            });
            return;
        }

        renderFallbackTable(result);
    }

    function renderFallbackTable(result) {
        selectors.grid.innerHTML = "";
        const columns = result.columns || [];
        const rows = result.rows || [];
        const header = columns.map(column => `<th>${escapeHtml(column.name)}</th>`).join("");
        const body = rows.map(row => {
            const cells = columns.map(column => `<td>${escapeHtml(formatCell(row[column.name]))}</td>`).join("");
            return `<tr>${cells}</tr>`;
        }).join("");

        selectors.fallback.innerHTML = `<table class="fallback-table"><thead><tr>${header}</tr></thead><tbody>${body}</tbody></table>`;
    }

    function selectedValues(select) {
        return [...select.selectedOptions].map(option => option.value).filter(Boolean);
    }

    function setSelections(select, values) {
        const lookup = new Set(values);
        [...select.options].forEach(option => {
            option.selected = lookup.has(option.value);
        });
    }

    async function fetchJson(url) {
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error(`Request failed: ${url}`);
        }

        return response.json();
    }

    function clearError() {
        selectors.errors.hidden = true;
        selectors.errors.innerHTML = "";
    }

    function showError(error) {
        const errors = Array.isArray(error) ? error : [error];
        selectors.errors.innerHTML = errors.map(item => `<div>${escapeHtml(String(item))}</div>`).join("");
        selectors.errors.hidden = false;
    }

    function formatCell(value) {
        if (value === null || value === undefined) {
            return "";
        }

        return typeof value === "object" ? JSON.stringify(value) : String(value);
    }

    function escapeHtml(value) {
        return String(value)
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }
})();
