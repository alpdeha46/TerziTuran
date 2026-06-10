(() => {
  "use strict";

  const root = document.documentElement;
  const body = document.body;
  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const path = window.location.pathname.toLowerCase();
  const palette = ["#7c3aed", "#06b6d4", "#10b981", "#f59e0b", "#ef476f", "#3b82f6", "#8b5cf6"];

  document.querySelectorAll(".menü-link").forEach(link => {
    const href = (link.getAttribute("href") || "").toLowerCase();
    if (href && path.startsWith(href)) link.classList.add("aktif-link");
  });

  const menuButton = document.querySelector(".mobil-menu");
  menuButton?.addEventListener("click", () => {
    const isOpen = body.classList.toggle("menu-open");
    menuButton.setAttribute("aria-expanded", String(isOpen));
  });
  document.querySelectorAll(".menü-link").forEach(link => link.addEventListener("click", () => body.classList.remove("menu-open")));

  document.querySelectorAll(".soft-card, .stat-card, .hero-panel, .ust-cubuk, .auth-card").forEach((element, index) => {
    element.classList.add("reveal-item");
    element.style.setProperty("--reveal-delay", `${Math.min(index * 45, 450)}ms`);
  });
  requestAnimationFrame(() => body.classList.add("sayfa-hazir"));

  document.querySelectorAll(".btn").forEach(button => {
    button.addEventListener("pointerdown", event => {
      const ripple = document.createElement("span");
      const rect = button.getBoundingClientRect();
      ripple.className = "button-ripple";
      ripple.style.left = `${event.clientX - rect.left}px`;
      ripple.style.top = `${event.clientY - rect.top}px`;
      button.appendChild(ripple);
      window.setTimeout(() => ripple.remove(), 650);
    });
  });

  const serviceTypeSelect = document.querySelector(".service-type-select");
  const categoryInput = document.querySelector(".category-input");
  if (serviceTypeSelect && categoryInput) {
    const sewingSamples = ["Takım Elbise", "Gömlek", "Abiye", "Ceket", "Pantolon"];
    const repairSamples = ["Fermuar Değişimi", "Paça Kısaltma", "Sökük Onarımı", "Astar Değişimi"];
    const syncPlaceholder = () => {
      categoryInput.placeholder = serviceTypeSelect.value === "Repair"
        ? repairSamples.join(" / ")
        : sewingSamples.join(" / ");
    };
    syncPlaceholder();
    serviceTypeSelect.addEventListener("change", syncPlaceholder);
  }

  const passwordInput = document.querySelector(".password-strength-source");
  const meter = document.querySelector(".password-meter span");
  passwordInput?.addEventListener("input", () => {
    const value = passwordInput.value;
    const score = [value.length >= 10, /[a-z]/.test(value), /[A-Z]/.test(value), /\d/.test(value), /[^A-Za-z0-9]/.test(value)]
      .filter(Boolean).length;
    meter?.style.setProperty("--password-score", `${score * 20}%`);
    meter?.setAttribute("data-score", String(score));
  });

  const animateNumber = element => {
    const original = element.textContent?.trim() || "";
    const numeric = Number(original.replace(/[^\d,-]/g, "").replace(/\./g, "").replace(",", "."));
    if (!Number.isFinite(numeric) || numeric === 0) return;
    const suffix = original.replace(/[\d.,\s]/g, "").trim();
    const duration = reducedMotion ? 1 : 900;
    const startedAt = performance.now();
    const step = now => {
      const progress = Math.min((now - startedAt) / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3);
      element.textContent = `${Math.round(numeric * eased).toLocaleString("tr-TR")}${suffix ? ` ${suffix}` : ""}`;
      if (progress < 1) requestAnimationFrame(step);
    };
    requestAnimationFrame(step);
  };
  const numberObserver = new IntersectionObserver(entries => {
    entries.filter(entry => entry.isIntersecting).forEach(entry => {
      animateNumber(entry.target);
      numberObserver.unobserve(entry.target);
    });
  }, { threshold: .45 });
  document.querySelectorAll(".stat-card strong").forEach(element => numberObserver.observe(element));

  const cssValue = name => getComputedStyle(body).getPropertyValue(name).trim();
  const roundRect = (ctx, x, y, width, height, radius) => {
    const r = Math.min(radius, width / 2, height / 2);
    ctx.beginPath();
    ctx.roundRect(x, y, width, height, r);
  };

  const renderChart = (canvas, type, values, progress = 1) => {
    const rect = canvas.getBoundingClientRect();
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    const width = Math.max(rect.width, 280);
    const height = Math.max(rect.height, 260);
    canvas.width = width * dpr;
    canvas.height = height * dpr;
    const ctx = canvas.getContext("2d");
    ctx.scale(dpr, dpr);
    ctx.clearRect(0, 0, width, height);

    if (!values?.length) {
      ctx.fillStyle = "#7b8497";
      ctx.font = "600 14px Manrope, sans-serif";
      ctx.textAlign = "center";
      ctx.fillText("Bu dönem için veri bulunmuyor.", width / 2, height / 2);
      return;
    }

    const accent = cssValue("--accent") || "#7c3aed";
    const accentSoft = cssValue("--accent-soft") || "rgba(124,58,237,.18)";
    const text = cssValue("--muted") || "#677085";
    const grid = "rgba(100, 116, 139, .14)";
    const max = Math.max(...values.map(item => Number(item.value) || 0), 1);
    const padding = { top: 18, right: 16, bottom: 42, left: 46 };
    const chartWidth = width - padding.left - padding.right;
    const chartHeight = height - padding.top - padding.bottom;

    if (type === "doughnut") {
      const total = values.reduce((sum, item) => sum + Number(item.value || 0), 0) || 1;
      const centerX = width / 2;
      const centerY = height / 2 - 10;
      const radius = Math.min(width, height) * .28;
      let start = -Math.PI / 2;
      values.forEach((item, index) => {
        const slice = (Number(item.value || 0) / total) * Math.PI * 2 * progress;
        ctx.beginPath();
        ctx.strokeStyle = palette[index % palette.length];
        ctx.lineWidth = Math.max(18, radius * .28);
        ctx.lineCap = "round";
        ctx.arc(centerX, centerY, radius, start, start + slice);
        ctx.stroke();
        start += slice;
      });
      ctx.fillStyle = accent;
      ctx.font = "800 28px Manrope, sans-serif";
      ctx.textAlign = "center";
      ctx.fillText(String(Math.round(total * progress)), centerX, centerY + 6);
      ctx.fillStyle = text;
      ctx.font = "600 12px Manrope, sans-serif";
      ctx.fillText("sipariş", centerX, centerY + 26);
      values.slice(0, 4).forEach((item, index) => {
        const x = 18 + (index % 2) * (width / 2);
        const y = height - 34 + Math.floor(index / 2) * 18;
        ctx.fillStyle = palette[index % palette.length];
        ctx.beginPath();
        ctx.arc(x, y - 4, 4, 0, Math.PI * 2);
        ctx.fill();
        ctx.fillStyle = text;
        ctx.textAlign = "left";
        ctx.font = "600 10px Manrope, sans-serif";
        ctx.fillText(String(item.label).slice(0, 16), x + 9, y);
      });
      return;
    }

    ctx.strokeStyle = grid;
    ctx.lineWidth = 1;
    ctx.fillStyle = text;
    ctx.font = "600 10px Manrope, sans-serif";
    for (let i = 0; i <= 4; i++) {
      const y = padding.top + chartHeight - (chartHeight / 4) * i;
      ctx.beginPath();
      ctx.moveTo(padding.left, y);
      ctx.lineTo(width - padding.right, y);
      ctx.stroke();
      ctx.textAlign = "right";
      ctx.fillText(Math.round((max / 4) * i).toLocaleString("tr-TR"), padding.left - 8, y + 3);
    }

    const slot = chartWidth / values.length;
    values.forEach((item, index) => {
      ctx.fillStyle = text;
      ctx.textAlign = "center";
      ctx.fillText(String(item.label), padding.left + slot * index + slot / 2, height - 16);
    });

    if (type === "bar") {
      values.forEach((item, index) => {
        const barHeight = (Number(item.value || 0) / max) * chartHeight * progress;
        const barWidth = Math.min(slot * .56, 54);
        const x = padding.left + slot * index + (slot - barWidth) / 2;
        const y = padding.top + chartHeight - barHeight;
        const gradient = ctx.createLinearGradient(0, y, 0, y + barHeight);
        gradient.addColorStop(0, accent);
        gradient.addColorStop(1, accentSoft);
        ctx.fillStyle = gradient;
        roundRect(ctx, x, y, barWidth, Math.max(barHeight, 3), 10);
        ctx.fill();
      });
      return;
    }

    const points = values.map((item, index) => ({
      x: padding.left + slot * index + slot / 2,
      y: padding.top + chartHeight - (Number(item.value || 0) / max) * chartHeight * progress
    }));
    const gradient = ctx.createLinearGradient(0, padding.top, 0, padding.top + chartHeight);
    gradient.addColorStop(0, accentSoft);
    gradient.addColorStop(1, "rgba(255,255,255,0)");
    ctx.beginPath();
    ctx.moveTo(points[0].x, padding.top + chartHeight);
    points.forEach(point => ctx.lineTo(point.x, point.y));
    ctx.lineTo(points.at(-1).x, padding.top + chartHeight);
    ctx.closePath();
    ctx.fillStyle = gradient;
    ctx.fill();
    ctx.beginPath();
    points.forEach((point, index) => index ? ctx.lineTo(point.x, point.y) : ctx.moveTo(point.x, point.y));
    ctx.strokeStyle = accent;
    ctx.lineWidth = 3;
    ctx.lineJoin = "round";
    ctx.stroke();
    points.forEach(point => {
      ctx.beginPath();
      ctx.fillStyle = "#fff";
      ctx.strokeStyle = accent;
      ctx.lineWidth = 3;
      ctx.arc(point.x, point.y, 5, 0, Math.PI * 2);
      ctx.fill();
      ctx.stroke();
    });
  };

  const startChart = (id, type, values) => {
    const canvas = document.getElementById(id);
    if (!canvas) return;
    canvas.setAttribute("role", "img");
    canvas.setAttribute("aria-label", `${type} grafik`);
    let startTime;
    const animate = timestamp => {
      startTime ??= timestamp;
      const progress = reducedMotion ? 1 : Math.min((timestamp - startTime) / 900, 1);
      renderChart(canvas, type, values, 1 - Math.pow(1 - progress, 3));
      if (progress < 1) requestAnimationFrame(animate);
    };
    requestAnimationFrame(animate);
    new ResizeObserver(() => renderChart(canvas, type, values)).observe(canvas.parentElement);
  };

  if (window.dashboardCharts) {
    startChart("ordersChart", "bar", window.dashboardCharts.orders);
    startChart("statusChart", "doughnut", window.dashboardCharts.status);
    startChart("revenueChart", "line", window.dashboardCharts.revenue);
  }
})();
