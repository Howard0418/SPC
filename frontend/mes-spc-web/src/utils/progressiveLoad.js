export async function loadPrimaryAndSupporting({
  loadPrimary,
  loadSupporting,
  onPrimaryLoaded = () => {},
  onSupportingLoaded = () => {},
  onPrimaryError = () => {},
  onSupportingError = () => {},
  onPrimarySettled = () => {},
  onSupportingSettled = () => {}
}) {
  const supportingPromise = Promise.resolve()
    .then(loadSupporting)
    .then(
      value => ({ ok: true, value }),
      error => ({ ok: false, error })
    );

  try {
    const primary = await loadPrimary();
    await onPrimaryLoaded(primary);
  } catch (error) {
    await onPrimaryError(error);
  } finally {
    await onPrimarySettled();
  }

  const supporting = await supportingPromise;
  try {
    if (supporting.ok) {
      await onSupportingLoaded(supporting.value);
    } else {
      await onSupportingError(supporting.error);
    }
  } finally {
    await onSupportingSettled();
  }
}
