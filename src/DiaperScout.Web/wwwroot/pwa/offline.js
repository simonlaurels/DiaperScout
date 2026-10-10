const status = document.getElementById('connection');
function connection() { status.textContent = navigator.onLine ? 'Your device is online. The service may still be unavailable.' : 'Your device is offline.'; }
document.getElementById('retry').addEventListener('click', () => location.reload());
addEventListener('online', connection);
addEventListener('offline', connection);
connection();
