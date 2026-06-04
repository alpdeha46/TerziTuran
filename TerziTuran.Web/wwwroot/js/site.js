(() => {
  const path = window.location.pathname.toLowerCase();
  document.querySelectorAll(".menü-link").forEach(link => {
    const href = (link.getAttribute("href") || "").toLowerCase();
    if (href && path.startsWith(href)) {
      link.classList.add("aktif-link");
    }
  });

  const serviceTypeSelect = document.querySelector(".service-type-select");
  const categoryInput = document.querySelector(".category-input");
  if (serviceTypeSelect && categoryInput) {
    const sewingSamples = ["Takim Elbise", "Gomlek", "Abiye", "Ceket", "Pantolon"];
    const repairSamples = ["Fermuar Degisimi", "Paca Kisaltma", "Sokuk Onarimi", "Astar Degisimi"];
    const syncPlaceholder = () => {
      categoryInput.placeholder = serviceTypeSelect.value === "Repair"
        ? repairSamples.join(" / ")
        : sewingSamples.join(" / ");
    };
    syncPlaceholder();
    serviceTypeSelect.addEventListener("change", syncPlaceholder);
  }

  if (window.dashboardCharts) {
    const render = (id, type, data, backgroundColor) => {
      const el = document.getElementById(id);
      if (!el) return;
      new Chart(el, {
        type,
        data: {
          labels: data.map(x => x.label),
          datasets: [{
            data: data.map(x => x.value),
            backgroundColor,
            borderColor: "#d4af37",
            tension: .35,
            fill: type === "line"
          }]
        },
        options: {
          responsive: true,
          plugins: { legend: { labels: { color: "#f8fafc" } } },
          scales: type === "pie" ? {} : {
            x: { ticks: { color: "#cbd5e1" }, grid: { color: "rgba(255,255,255,.08)" } },
            y: { ticks: { color: "#cbd5e1" }, grid: { color: "rgba(255,255,255,.08)" } }
          }
        }
      });
    };
    render("ordersChart", "bar", window.dashboardCharts.orders, "rgba(212,175,55,.72)");
    render("statusChart", "doughnut", window.dashboardCharts.status, ["#d4af37","#60a5fa","#34d399","#f472b6","#f87171","#818cf8","#facc15"]);
    render("revenueChart", "line", window.dashboardCharts.revenue, "rgba(96,165,250,.75)");
  }
})();
