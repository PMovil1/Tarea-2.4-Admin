package hn.uth.bitacora;

import org.json.JSONObject;
import java.io.ByteArrayOutputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import javax.net.ssl.HttpsURLConnection;

public final class ApiClient {
    public static final class Result {
        public final int code;
        public final JSONObject body;
        Result(int code, JSONObject body) { this.code = code; this.body = body; }
    }

    private HttpsURLConnection connection;

    private synchronized HttpsURLConnection open(String path) throws Exception {
        URL base = new URL(BuildConfig.API_BASE_URL.replaceAll("/+$", "") + path);
        if (!"https".equals(base.getProtocol()) || base.getHost().isEmpty()
                || base.getUserInfo() != null || base.getQuery() != null || base.getRef() != null) {
            throw new IllegalArgumentException();
        }
        connection = (HttpsURLConnection) base.openConnection();
        connection.setConnectTimeout(15000);
        connection.setReadTimeout(15000);
        connection.setInstanceFollowRedirects(false);
        connection.setRequestProperty("Accept", "application/json");
        return connection;
    }

    public Result login(String email, String password) throws Exception {
        HttpsURLConnection request = open("/api/auth/login");
        try {
            request.setRequestMethod("POST");
            request.setDoOutput(true);
            request.setRequestProperty("Content-Type", "application/json; charset=UTF-8");
            byte[] payload = new JSONObject().put("email", email).put("password", password)
                    .toString().getBytes(StandardCharsets.UTF_8);
            request.setFixedLengthStreamingMode(payload.length);
            try (OutputStream stream = request.getOutputStream()) { stream.write(payload); }
            int code = request.getResponseCode();
            if (code != 200) return new Result(code, null);
            try (InputStream input = request.getInputStream();
                 ByteArrayOutputStream output = new ByteArrayOutputStream()) {
                byte[] buffer = new byte[4096];
                int read;
                while ((read = input.read(buffer)) != -1) {
                    if (output.size() + read > 65536) throw new IllegalArgumentException();
                    output.write(buffer, 0, read);
                }
                return new Result(code, new JSONObject(output.toString(StandardCharsets.UTF_8.name())));
            }
        } finally { request.disconnect(); }
    }

    public int verify(String token) throws Exception {
        HttpsURLConnection request = open("/api/registros");
        try {
            request.setRequestProperty("Authorization", "Bearer " + token);
            return request.getResponseCode();
        } finally { request.disconnect(); }
    }

    public synchronized void cancel() {
        if (connection != null) connection.disconnect();
    }
}
