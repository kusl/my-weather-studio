const modal = document.getElementById("components-reconnect-modal");

const retryWhenVisible = async () => {
  if (document.visibilityState === "visible") {
    await retry();
  }
};

async function retry() {
  document.removeEventListener("visibilitychange", retryWhenVisible);
  try {
    if (await Blazor.reconnect()) {
      return;
    }

    if (await Blazor.resumeCircuit()) {
      modal.close();
    } else {
      location.reload();
    }
  } catch {
    document.addEventListener("visibilitychange", retryWhenVisible);
  }
}

async function resume() {
  try {
    if (!(await Blazor.resumeCircuit())) {
      modal.classList.replace("components-reconnect-paused", "components-reconnect-resume-failed");
    }
  } catch {
    modal.classList.replace("components-reconnect-paused", "components-reconnect-resume-failed");
  }
}

modal.addEventListener("components-reconnect-state-changed", ({ detail }) => {
  switch (detail.state) {
    case "show":
      modal.showModal();
      break;
    case "hide":
      modal.close();
      break;
    case "failed":
      document.addEventListener("visibilitychange", retryWhenVisible);
      break;
    case "rejected":
      location.reload();
      break;
  }
});

document.getElementById("components-reconnect-button").addEventListener("click", retry);
document.getElementById("components-resume-button").addEventListener("click", resume);
