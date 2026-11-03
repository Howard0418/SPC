/** Reports a logout request at most once; local logout always completes within the bound. */
export function createLogoutHandler({ report, clear, redirect, timeoutMs = 1500 }) {
  let started = false;
  return async () => {
    if (started) return;
    started = true;
    let timer;
    try {
      await Promise.race([
        Promise.resolve().then(report),
        new Promise((resolve) => { timer = setTimeout(resolve, timeoutMs); })
      ]);
    } catch {
      // The user must still be able to leave when the network or audit service is unavailable.
    } finally {
      clearTimeout(timer);
      clear();
      redirect();
    }
  };
}
