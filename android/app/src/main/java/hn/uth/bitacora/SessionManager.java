package hn.uth.bitacora;

import android.content.Context;
import android.content.SharedPreferences;
import android.util.Base64;
import org.json.JSONObject;
import java.nio.charset.StandardCharsets;
import java.time.Instant;

public final class SessionManager {
    private final SharedPreferences preferences;

    public SessionManager(Context context) {
        preferences = context.getSharedPreferences("private_session", Context.MODE_PRIVATE);
    }

    public void save(JSONObject response) throws Exception {
        String token = response.getString("token").trim();
        String email = response.getString("email").trim();
        String role = response.getString("role").trim();
        long expiration = Instant.parse(response.getString("expiration")).toEpochMilli();
        String[] parts = token.split("\\.", -1);
        if (parts.length != 3 || parts[0].isEmpty() || parts[1].isEmpty() || parts[2].isEmpty()
                || email.isEmpty() || role.isEmpty()) {
            throw new IllegalArgumentException();
        }
        JSONObject claims = new JSONObject(new String(Base64.decode(parts[1], Base64.URL_SAFE), StandardCharsets.UTF_8));
        long jwtExpiration = Math.multiplyExact(claims.getLong("exp"), 1000L);
        expiration = Math.min(expiration, jwtExpiration);
        if (expiration <= System.currentTimeMillis()) throw new IllegalArgumentException();
        if (!preferences.edit().putString("token", token).putString("email", email)
                .putString("role", role).putLong("expiration", expiration).commit()) {
            clear();
            throw new IllegalStateException();
        }
    }

    public boolean isValid() {
        return !token().isEmpty() && remainingMillis() > 0;
    }

    public long remainingMillis() {
        return preferences.getLong("expiration", 0) - System.currentTimeMillis();
    }

    public String token() { return preferences.getString("token", ""); }
    public String email() { return preferences.getString("email", ""); }
    public String role() { return preferences.getString("role", ""); }
    public void clear() { preferences.edit().clear().apply(); }
}
