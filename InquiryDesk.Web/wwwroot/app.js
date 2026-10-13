// 询盘跟进台前端逻辑：原生 JS + fetch，没有构建步骤，改完刷新页面即可。
// 服务端返回的枚举都是英文名，这里集中翻译成中文显示。

const StatusText = {
    New: "新询盘",
    Contacted: "已联系",
    FollowingUp: "跟进中",
    Won: "已成交",
    Lost: "已流失"
};

const IntentText = {
    High: "高",
    Medium: "中",
    Low: "低"
};

const SourceText = {
    Website: "网站",
    Exhibition: "展会",
    Referral: "转介绍",
    Phone: "电话",
    Other: "其他"
};

// 当前打开详情的询盘编号；没有打开详情时为 null
let currentDetailId = null;

function $(id) {
    return document.getElementById(id);
}

// 用户输入的内容拼进 HTML 前必须转义，防止把页面结构冲坏
function escapeHtml(text) {
    if (text === null || text === undefined) {
        return "";
    }
    return String(text)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
}

// 服务端的时间形如 "2026-10-13T15:04:05"，直接截取显示，避免时区换算带来的困惑
function formatDateTime(isoText) {
    if (!isoText) {
        return "—";
    }
    return isoText.replace("T", " ").substring(0, 16);
}

// 统一的接口调用：失败时把服务端给的原因（error 字段）抛出来，由调用方 alert
async function callApi(path, options) {
    const response = await fetch(path, options);
    if (!response.ok) {
        let message = "请求失败（HTTP " + response.status + "）";
        try {
            const body = await response.json();
            if (body && body.error) {
                message = body.error;
            }
        } catch (ignore) {
            // 响应不是 JSON 时就用默认消息
        }
        throw new Error(message);
    }
    return response.json();
}

function jsonOptions(method, bodyObject) {
    return {
        method: method,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(bodyObject)
    };
}

// ---------- 统计卡片 ----------

async function loadStats() {
    const stats = await callApi("/api/stats");
    const cards = [
        { label: "询盘总数", value: stats.total, cssClass: "" },
        { label: "新询盘", value: stats.byStatus.New || 0, cssClass: "" },
        { label: "已联系", value: stats.byStatus.Contacted || 0, cssClass: "" },
        { label: "跟进中", value: stats.byStatus.FollowingUp || 0, cssClass: "" },
        { label: "已成交", value: stats.byStatus.Won || 0, cssClass: "" },
        { label: "已流失", value: stats.byStatus.Lost || 0, cssClass: "" },
        { label: "今日待跟", value: stats.dueTodayCount, cssClass: "warn" },
        { label: "已逾期", value: stats.overdueCount, cssClass: "danger" }
    ];

    let html = "";
    for (const card of cards) {
        html += '<div class="card ' + card.cssClass + '">'
            + '<div class="num">' + card.value + "</div>"
            + '<div class="label">' + card.label + "</div></div>";
    }
    $("statCards").innerHTML = html;
}

// ---------- 到期提醒 ----------

function dueBadge(dueKind) {
    if (dueKind === "Overdue") {
        return '<span class="badge badge-overdue">已逾期</span>';
    }
    if (dueKind === "DueToday") {
        return '<span class="badge badge-today">今日待跟</span>';
    }
    return "";
}

async function loadDue() {
    const items = await callApi("/api/inquiries/due");
    if (items.length === 0) {
        $("dueList").innerHTML = '<p class="hint">暂无到期提醒，手头都跟上了。</p>';
        return;
    }
    let html = "";
    for (const item of items) {
        const cssClass = item.dueKind === "Overdue" ? "overdue" : "today";
        html += '<div class="due-item ' + cssClass + '" data-id="' + item.id + '">'
            + "<strong>" + escapeHtml(item.customerName) + "</strong> "
            + dueBadge(item.dueKind) + " "
            + '<span class="hint">状态：' + StatusText[item.status]
            + "，下次跟进：" + formatDateTime(item.nextFollowUpAt) + "</span></div>";
    }
    $("dueList").innerHTML = html;

    // 点提醒直接打开对应详情
    const nodes = $("dueList").querySelectorAll(".due-item");
    for (const node of nodes) {
        node.addEventListener("click", function () {
            openDetail(Number(node.getAttribute("data-id")));
        });
    }
}

// ---------- 询盘列表 ----------

async function loadList() {
    const status = $("filterStatus").value;
    const intent = $("filterIntent").value;
    const params = new URLSearchParams();
    if (status) {
        params.set("status", status);
    }
    if (intent) {
        params.set("intent", intent);
    }
    const query = params.toString();
    const items = await callApi("/api/inquiries" + (query ? "?" + query : ""));

    let html = "";
    for (const item of items) {
        html += "<tr data-id=\"" + item.id + "\">"
            + "<td><strong>" + escapeHtml(item.customerName) + "</strong></td>"
            + "<td>" + escapeHtml(item.contactInfo) + "</td>"
            + "<td>" + SourceText[item.source] + "</td>"
            + '<td class="content-cell">' + escapeHtml(item.content) + "</td>"
            + "<td>" + IntentText[item.intent] + "</td>"
            + "<td>" + StatusText[item.status] + "</td>"
            + "<td>" + formatDateTime(item.nextFollowUpAt) + " " + dueBadge(item.dueKind) + "</td>"
            + "<td>" + formatDateTime(item.createdAt) + "</td>"
            + "</tr>";
    }
    $("inquiryTableBody").innerHTML = html;
    $("emptyHint").hidden = items.length > 0;

    const rows = $("inquiryTableBody").querySelectorAll("tr");
    for (const row of rows) {
        row.addEventListener("click", function () {
            openDetail(Number(row.getAttribute("data-id")));
        });
    }
}

// ---------- 新建询盘 ----------

function readDateTimeInput(inputId) {
    const value = $(inputId).value;
    // datetime-local 为空时传 null，表示不设置下次跟进时间
    return value ? value : null;
}

async function submitCreate(event) {
    event.preventDefault();
    const payload = {
        customerName: $("createCustomerName").value,
        contactInfo: $("createContactInfo").value,
        source: $("createSource").value,
        intent: $("createIntent").value,
        content: $("createContent").value,
        nextFollowUpAt: readDateTimeInput("createNextFollowUpAt")
    };
    try {
        const created = await callApi("/api/inquiries", jsonOptions("POST", payload));
        $("createForm").reset();
        $("createForm").hidden = true;
        await refreshAll();
        openDetail(created.id);
    } catch (error) {
        alert(error.message);
    }
}

// ---------- 详情 ----------

async function openDetail(id) {
    currentDetailId = id;
    const detail = await callApi("/api/inquiries/" + id);
    renderDetail(detail);
    $("detailSection").hidden = false;
    $("detailSection").scrollIntoView({ behavior: "smooth" });
}

function renderDetail(detail) {
    $("detailTitle").textContent = "询盘详情 #" + detail.id;
    $("detailInfo").innerHTML =
        "<p><strong>客户：</strong>" + escapeHtml(detail.customerName)
        + "（" + escapeHtml(detail.contactInfo) + "）</p>"
        + "<p><strong>来源：</strong>" + SourceText[detail.source]
        + "　<strong>意向：</strong>" + IntentText[detail.intent]
        + "　<strong>状态：</strong>" + StatusText[detail.status] + "</p>"
        + "<p><strong>内容：</strong>" + escapeHtml(detail.content) + "</p>"
        + "<p><strong>下次跟进：</strong>" + formatDateTime(detail.nextFollowUpAt)
        + " " + dueBadge(detail.dueKind)
        + "　<strong>创建时间：</strong>" + formatDateTime(detail.createdAt) + "</p>";
    $("detailStatusSelect").value = detail.status;

    if (detail.notes.length === 0) {
        $("noteList").innerHTML = '<p class="hint">还没有跟进记录。</p>';
        return;
    }
    let html = "";
    for (const note of detail.notes) {
        html += '<div class="note-item">'
            + '<div class="note-time">' + formatDateTime(note.createdAt) + "</div>"
            + "<div>" + escapeHtml(note.content) + "</div></div>";
    }
    $("noteList").innerHTML = html;
}

async function submitChangeStatus() {
    if (currentDetailId === null) {
        return;
    }
    const payload = { status: $("detailStatusSelect").value };
    try {
        const detail = await callApi(
            "/api/inquiries/" + currentDetailId + "/status",
            jsonOptions("PUT", payload)
        );
        renderDetail(detail);
        await refreshListsOnly();
    } catch (error) {
        alert(error.message);
    }
}

async function submitNote(event) {
    event.preventDefault();
    if (currentDetailId === null) {
        return;
    }
    const payload = {
        content: $("noteContent").value,
        nextFollowUpAt: readDateTimeInput("noteNextFollowUpAt")
    };
    try {
        const detail = await callApi(
            "/api/inquiries/" + currentDetailId + "/notes",
            jsonOptions("POST", payload)
        );
        $("noteForm").reset();
        renderDetail(detail);
        await refreshListsOnly();
    } catch (error) {
        alert(error.message);
    }
}

// ---------- 页面初始化 ----------

async function refreshListsOnly() {
    await loadStats();
    await loadDue();
    await loadList();
}

async function refreshAll() {
    await refreshListsOnly();
}

document.addEventListener("DOMContentLoaded", function () {
    $("filterStatus").addEventListener("change", loadList);
    $("filterIntent").addEventListener("change", loadList);
    $("exportButton").addEventListener("click", function () {
        window.location.href = "/api/inquiries/export";
    });
    $("toggleCreateButton").addEventListener("click", function () {
        $("createForm").hidden = !$("createForm").hidden;
    });
    $("cancelCreateButton").addEventListener("click", function () {
        $("createForm").hidden = true;
    });
    $("createForm").addEventListener("submit", submitCreate);
    $("changeStatusButton").addEventListener("click", submitChangeStatus);
    $("noteForm").addEventListener("submit", submitNote);
    $("closeDetailButton").addEventListener("click", function () {
        $("detailSection").hidden = true;
        currentDetailId = null;
    });

    refreshAll().catch(function (error) {
        alert("加载数据失败：" + error.message);
    });
});
