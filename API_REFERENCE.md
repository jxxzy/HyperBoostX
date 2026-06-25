# HyperBoost X - REST API Reference Guide

Complete API documentation for all backend endpoints and services.

## 📡 API Overview

| Property | Value |
|----------|-------|
| **Base URL** | `http://127.0.0.1:5000` |
| **Protocol** | HTTP/REST |
| **Data Format** | JSON |
| **Default Port** | 5000 |
| **CORS** | Enabled for localhost |
| **Authentication** | `X-HyperBoostX-Token` local backend token |

---

## HyperBoostX Triple AI Engine

Core flow:

`Scan PC -> AI Analyzer -> AI Safety Guard -> AI Assistant -> User Approval -> Safe Tweak Engine -> Backup/Revert -> Performance Report`

All endpoints require `X-HyperBoostX-Token`.

| Endpoint | Purpose |
|----------|---------|
| `POST /scan` | Run local PC scanner. |
| `POST /ai/analyze` | Analyze a scan result and return structured issues/recommendations. |
| `POST /ai/safety-check` | Approve, warn, or block recommendations before apply. |
| `POST /api/triple-ai/full-flow` | Run scan, analyze, safety, assistant, and report without applying tweaks. |
| `POST /tweaks/apply` | Apply only Safety Guard approved tweaks after `user_approved: true`. |
| `POST /tweaks/revert` | Revert previously applied tweak IDs or backup context. |
| `POST /game/optimize` | Return safe manual game/NVIDIA setting recommendations. |

Aliases are also exposed under `/api/triple-ai/*` and `/api/hyperboostx/*`.

Safety policy: HyperBoostX blocks overclock, undervolt, voltage/BIOS/UEFI changes, disabling Windows Security, permanent Windows Update disable, irreversible registry edits, and guaranteed FPS claims.

---

## 🏥 Health Check Endpoint

### GET /api/health
Check if backend server is running and responsive.

**Request:**
```http
GET /api/health HTTP/1.1
Host: 127.0.0.1:5000
```

**Response (200):**
```json
{
    "success": true,
    "message": "Backend server is running",
    "data": {
        "status": "online",
        "version": "1.0.0",
        "uptime": 3600
    }
}
```

**Usage:**
```python
import requests
response = requests.get('http://127.0.0.1:5000/api/health')
if response.status_code == 200:
    print("Backend is online")
```

---

## 🖥️ System Endpoints (/api/system/*)

### GET /api/system/info
Get complete system information and hardware details.

**Request:**
```http
GET /api/system/info HTTP/1.1
Host: 127.0.0.1:5000
```

**Response (200):**
```json
{
    "success": true,
    "message": "System information retrieved",
    "data": {
        "os": {
            "name": "Windows 11",
            "version": "22H2",
            "build": 22621
        },
        "cpu": {
            "brand": "Intel Core i7-12700K",
            "cores": 12,
            "threads": 20,
            "frequency": "3.6 GHz"
        },
        "ram": {
            "total_gb": 32,
            "available_gb": 28,
            "usage_percent": 12.5
        },
        "gpu": {
            "model": "NVIDIA GeForce RTX 3080",
            "vram_gb": 10
        },
        "disk": {
            "total_gb": 1000,
            "free_gb": 450,
            "usage_percent": 55
        },
        "motherboard": "ASUS ROG STRIX Z790",
        "psu_wattage": 850
    }
}
```

**File Location:** `app/services/system_info_service.py`

---

### GET /api/system/monitor
Get real-time system statistics (CPU, memory, disk, network).

**Request:**
```http
GET /api/system/monitor HTTP/1.1
Host: 127.0.0.1:5000
```

**Response (200):**
```json
{
    "success": true,
    "message": "Current system stats",
    "data": {
        "cpu": {
            "usage_percent": 23.4,
            "per_core": [15.2, 28.3, 19.5, 31.2, ...],
            "temperature": 52.3
        },
        "memory": {
            "usage_gb": 12.5,
            "total_gb": 32,
            "usage_percent": 39.1
        },
        "disk": {
            "usage_gb": 550,
            "total_gb": 1000,
            "usage_percent": 55,
            "read_speed_mbs": 120.5,
            "write_speed_mbs": 85.3
        },
        "gpu": {
            "usage_percent": 45,
            "temperature": 62,
            "vram_usage_gb": 4.2
        },
        "network": {
            "bytes_sent": 1024000,
            "bytes_received": 2048000,
            "download_mbps": 5.2,
            "upload_mbps": 2.1
        },
        "processes": {
            "total": 245,
            "running": 198,
            "top_cpu": "HyperBoostX.exe (12.5%)"
        }
    }
}
```

**File Location:** `app/services/monitor_service.py`

---

### GET /api/system/processes
Get list of running processes with resource usage.

**Request:**
```http
GET /api/system/processes?sort=cpu&limit=20 HTTP/1.1
Host: 127.0.0.1:5000
```

**Query Parameters:**
- `sort` - `cpu`, `memory`, `name` (default: cpu)
- `limit` - Number of processes to return (default: 20)

**Response (200):**
```json
{
    "success": true,
    "message": "Processes retrieved",
    "data": {
        "processes": [
            {
                "pid": 1234,
                "name": "chrome.exe",
                "cpu_percent": 15.2,
                "memory_mb": 850.5,
                "status": "running"
            }
        ]
    }
}
```

---

## 🎮 Booster Endpoints (/api/booster/*)

### GET /api/booster/profiles
Get all available optimization profiles.

**Request:**
```http
GET /api/booster/profiles HTTP/1.1
Host: 127.0.0.1:5000
```

**Response (200):**
```json
{
    "success": true,
    "message": "Profiles retrieved",
    "data": {
        "profiles": [
            {
                "id": "fps_mode",
                "name": "FPS Mode",
                "description": "Maximum gaming performance",
                "priority": "high",
                "optimizations": {
                    "cpu_affinity": true,
                    "memory_optimization": true,
                    "background_tasks": false,
                    "power_plan": "High Performance"
                }
            },
            {
                "id": "low_latency",
                "name": "Low Latency Mode",
                "description": "Minimize network lag",
                "priority": "high",
                "optimizations": {
                    "network_priority": true,
                    "latency_reduction": true,
                    "power_plan": "High Performance"
                }
            },
            {
                "id": "streaming",
                "name": "Streaming Mode",
                "description": "Optimized for content creation",
                "priority": "balanced"
            },
            {
                "id": "balanced",
                "name": "Balanced Mode",
                "description": "Balanced performance and power usage",
                "priority": "balanced"
            }
        ]
    }
}
```

---

### POST /api/booster/apply
Apply a specific optimization profile.

**Request:**
```http
POST /api/booster/apply HTTP/1.1
Host: 127.0.0.1:5000
Content-Type: application/json

{
    "profile_id": "fps_mode",
    "aggressive": false
}
```

**Request Body:**
```json
{
    "profile_id": "fps_mode|low_latency|streaming|balanced",
    "aggressive": false
}
```

**Response (200):**
```json
{
    "success": true,
    "message": "Profile applied successfully",
    "data": {
        "profile_applied": "fps_mode",
        "changes_made": 15,
        "estimated_fps_increase": "8-15%",
        "requires_restart": false
    }
}
```

---

### GET /api/booster/status
Get current booster status and active profile.

**Request:**
```http
GET /api/booster/status HTTP/1.1
Host: 127.0.0.1:5000
```

**Response (200):**
```json
{
    "success": true,
    "message": "Status retrieved",
    "data": {
        "active_profile": "fps_mode",
        "status": "active",
        "applied_at": "2026-04-06T22:30:00",
        "optimizations_active": 12,
        "performance_improvement": "12.5%"
    }
}
```

---

### POST /api/booster/revert
Revert all booster optimizations.

**Request:**
```http
POST /api/booster/revert HTTP/1.1
Host: 127.0.0.1:5000
```

**Response (200):**
```json
{
    "success": true,
    "message": "Optimizations reverted",
    "data": {
        "changes_reverted": 12,
        "requires_restart": false
    }
}
```

---

## 🛠️ Tweaks Endpoints (/api/tweaks/*)

### GET /api/tweaks
Get all available Windows tweaks.

**Request:**
```http
GET /api/tweaks?category=performance&risk=low HTTP/1.1
Host: 127.0.0.1:5000
```

**Query Parameters:**
- `category` - `performance`, `privacy`, `ui`, `security`, etc.
- `risk` - `low`, `medium`, `high`
- `search` - Search term in name/description

**Response (200):**
```json
{
    "success": true,
    "message": "Tweaks retrieved",
    "data": {
        "tweaks": [
            {
                "id": "tweak_001",
                "name": "Disable Animations",
                "description": "Speeds up Windows animations and interactions",
                "category": "performance",
                "risk": "low",
                "action": "registry",
                "registry_path": "HKEY_CURRENT_USER\\Control Panel\\Desktop",
                "key": "UserPreferencesMask",
                "value": "9012038E",
                "reversible": true
            },
            {
                "id": "tweak_002",
                "name": "Disable Background Apps",
                "description": "Stop unnecessary background applications",
                "category": "performance",
                "risk": "low",
                "enabled": false
            }
        ]
    }
}
```

---

### POST /api/tweaks/apply
Apply one or more tweaks.

**Request:**
```http
POST /api/tweaks/apply HTTP/1.1
Host: 127.0.0.1:5000
Content-Type: application/json

{
    "tweak_ids": ["tweak_001", "tweak_002"],
    "create_restore_point": true
}
```

**Request Body:**
```json
{
    "tweak_ids": ["tweak_001", "tweak_002"],
    "create_restore_point": true
}
```

**Response (200):**
```json
{
    "success": true,
    "message": "Tweaks applied successfully",
    "data": {
        "applied_count": 2,
        "failed_count": 0,
        "restore_point_created": true,
        "restart_required": false,
        "applied_tweaks": ["tweak_001", "tweak_002"]
    }
}
```

---

### POST /api/tweaks/revert
Revert previously applied tweaks.

**Request:**
```http
POST /api/tweaks/revert HTTP/1.1
Host: 127.0.0.1:5000
Content-Type: application/json

{
    "tweak_ids": ["tweak_001"],
    "restore_point_id": "rp_12345"
}
```

**Response (200):**
```json
{
    "success": true,
    "message": "Tweaks reverted successfully",
    "data": {
        "reverted_count": 1,
        "failed_count": 0
    }
}
```

---

## 🖨️ Driver Endpoints

### GET /api/drivers
Get installed drivers and their information.

**Request:**
```http
GET /api/drivers HTTP/1.1
Host: 127.0.0.1:5000
```

**Response (200):**
```json
{
    "success": true,
    "message": "Drivers retrieved",
    "data": {
        "drivers": [
            {
                "id": "driver_nvidia",
                "name": "NVIDIA GeForce RTX 3080",
                "category": "GPU",
                "current_version": "531.0",
                "latest_version": "533.2",
                "status": "outdated",
                "update_available": true,
                "release_date": "2023-04-10"
            },
            {
                "id": "driver_intel_net",
                "name": "Intel Network Adapter",
                "category": "Network",
                "current_version": "24.1",
                "latest_version": "24.1",
                "status": "updated"
            }
        ]
    }
}
```

---

### POST /api/drivers/check-updates
Check for driver updates.

**Request:**
```http
POST /api/drivers/check-updates HTTP/1.1
Host: 127.0.0.1:5000
```

**Response (200):**
```json
{
    "success": true,
    "message": "Update check completed",
    "data": {
        "updates_available": 2,
        "drivers_to_update": [
            {
                "name": "NVIDIA GeForce RTX 3080",
                "current": "531.0",
                "new": "533.2"
            }
        ]
    }
}
```

---

## 🔧 Repair Endpoints

### POST /api/repair/sfc-scan
Run System File Checker scan.

**Request:**
```http
POST /api/repair/sfc-scan HTTP/1.1
Host: 127.0.0.1:5000
```

**Response (200):**
```json
{
    "success": true,
    "message": "SFC scan started",
    "data": {
        "status": "running",
        "scan_id": "sfc_20260406_223000",
        "estimated_time": "15-30 minutes",
        "progress_url": "/api/repair/sfc-scan/sfc_20260406_223000/progress"
    }
}
```

---

### POST /api/repair/dism-repair
Run DISM repair.

**Request:**
```http
POST /api/repair/dism-repair HTTP/1.1
Host: 127.0.0.1:5000
```

**Response (200):**
```json
{
    "success": true,
    "message": "DISM repair started",
    "data": {
        "status": "running",
        "repair_id": "dism_20260406_223000",
        "estimated_time": "30-60 minutes"
    }
}
```

---

### POST /api/repair/cleanup
Clean temporary files and cache.

**Request:**
```http
POST /api/repair/cleanup HTTP/1.1
Host: 127.0.0.1:5000
Content-Type: application/json

{
    "targets": ["temp", "cache", "logs"],
    "safe_mode": true
}
```

**Response (200):**
```json
{
    "success": true,
    "message": "Cleanup completed",
    "data": {
        "files_deleted": 1250,
        "space_freed_mb": 2450.5
    }
}
```

---

## 🌐 Network Endpoints

### GET /api/network/info
Get network information and configuration.

**Request:**
```http
GET /api/network/info HTTP/1.1
Host: 127.0.0.1:5000
```

**Response (200):**
```json
{
    "success": true,
    "message": "Network info retrieved",
    "data": {
        "ip_address": "192.168.1.100",
        "gateway": "192.168.1.1",
        "dns": ["8.8.8.8", "1.1.1.1"],
        "connection_type": "Ethernet",
        "speed_mbps": 1000,
        "mac_address": "00:1A:2B:3C:4D:5E"
    }
}
```

---

### POST /api/network/optimize
Run network optimization.

**Request:**
```http
POST /api/network/optimize HTTP/1.1
Host: 127.0.0.1:5000
Content-Type: application/json

{
    "optimizations": ["dns_flush", "tcp_tuning", "buffer_optimize"]
}
```

**Response (200):**
```json
{
    "success": true,
    "message": "Network optimized",
    "data": {
        "optimizations_applied": 3,
        "latency_improvement": "5-10ms",
        "requires_restart": false
    }
}
```

---

## 📊 Response Format

### Success Response (2xx)
```json
{
    "success": true,
    "message": "Human-readable success message",
    "data": {
        // Response-specific data
    }
}
```

### Error Response (4xx-5xx)
```json
{
    "success": false,
    "error": "Error code",
    "message": "Human-readable error message",
    "details": {
        // Optional error details
    }
}
```

### Error Codes
| Code | Status | Meaning |
|------|--------|---------|
| `INVALID_REQUEST` | 400 | Invalid request format or missing parameters |
| `NOT_FOUND` | 404 | Resource not found |
| `PERMISSION_DENIED` | 403 | Operation requires admin privileges |
| `SERVICE_UNAVAILABLE` | 503 | Backend service temporarily unavailable |
| `INTERNAL_ERROR` | 500 | Unexpected server error |

---

## 🔌 WebSocket Streaming

### Connection
```javascript
ws = new WebSocket('ws://127.0.0.1:5000/api/monitor/stream');

ws.onmessage = function(event) {
    const stats = JSON.parse(event.data);
    console.log('CPU:', stats.cpu.usage_percent);
    console.log('Memory:', stats.memory.usage_percent);
};
```

### Message Format
```json
{
    "timestamp": "2026-04-06T22:30:00Z",
    "cpu": {
        "usage_percent": 23.4,
        "temperature": 52.3
    },
    "memory": {
        "usage_percent": 39.1
    },
    "disk": {
        "usage_percent": 55
    }
}
```

---

## 💻 Client Integration Examples

### Python (using requests)
```python
import requests
import json

# Health check
response = requests.get('http://127.0.0.1:5000/api/health')
print(response.json())

# Apply booster
payload = {"profile_id": "fps_mode"}
response = requests.post(
    'http://127.0.0.1:5000/api/booster/apply',
    json=payload
)
print(response.json())
```

### C# (HttpClient)
```csharp
using System.Net.Http;
using System.Text.Json;

// Health check
var client = new HttpClient();
var response = await client.GetAsync("http://127.0.0.1:5000/api/health");
var json = await response.Content.ReadAsAsync<dynamic>();
```

### JavaScript (fetch)
```javascript
// Health check
fetch('http://127.0.0.1:5000/api/health')
    .then(r => r.json())
    .then(data => console.log(data));

// Apply booster
fetch('http://127.0.0.1:5000/api/booster/apply', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({profile_id: 'fps_mode'})
})
    .then(r => r.json())
    .then(data => console.log(data));
```

---

## 🧪 Testing with Postman

1. Import this collection into Postman
2. Set environment variable: `base_url` = `http://127.0.0.1:5000`
3. Start backend server
4. Run requests from your collection

**Example URL:** `{{base_url}}/api/health`

---

## 📝 Notes

- Most endpoints require the backend server running on port 5000
- Admin privileges required for system-modifying operations
- All times in responses are UTC (ISO 8601 format)
- Request/response encoding is UTF-8 JSON
- No API rate limiting (local development only)

---

*API Documentation v1.0*  
*Last Updated: April 6, 2026*
