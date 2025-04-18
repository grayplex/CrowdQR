# Redis Key Schema

| Key Pattern | Data Type | TTL | Purpose |
|-------------|-----------|-----|---------|
| `votes:{RequestID}` | Integer | None | Tracks current vote count for a request |
| `user:{UserID}:voted:{RequestID}` | String | 600s | Marks that user has voted (for deduping) |
| `rate:{UserID}` | Integer | 60s | Tracks rate of submissions |
| `active_clients:{EventID}` | Integer | None | Number of connected clients (for dashboard) |
| `lock:vote:{UserID}:{RequestID}` | String | 5s | Prevents rapid duplicate voting |
