package hn.uth.bitacora;

import android.content.Intent;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.view.View;
import android.view.WindowManager;
import android.widget.TextView;
import androidx.appcompat.app.AppCompatActivity;
import java.io.IOException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public final class MainActivity extends AppCompatActivity {
    private SessionManager session;
    private TextView status, identity;
    private View progress, retry;
    private final ApiClient api = new ApiClient();
    private final ExecutorService executor = Executors.newSingleThreadExecutor();
    private final Handler handler = new Handler(Looper.getMainLooper());
    private final Runnable expire = () -> leave(true);
    private boolean verifying;

    @Override protected void onCreate(Bundle state) {
        super.onCreate(state);
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_SECURE);
        session = new SessionManager(this);
        setContentView(R.layout.activity_main);
        ScreenInsets.apply(this);
        status = findViewById(R.id.status);
        identity = findViewById(R.id.identity);
        progress = findViewById(R.id.progress);
        retry = findViewById(R.id.retryButton);
        retry.setVisibility(View.GONE);
        retry.setOnClickListener(view -> verify());
        findViewById(R.id.logoutButton).setOnClickListener(view -> leave(false));
    }

    @Override protected void onResume() {
        super.onResume();
        if (!session.isValid()) { leave(true); return; }
        handler.removeCallbacks(expire);
        handler.postDelayed(expire, session.remainingMillis());
        verify();
    }

    private void verify() {
        if (verifying || isFinishing()) return;
        if (!session.isValid()) { leave(true); return; }
        verifying = true;
        identity.setText("");
        status.setText(R.string.verifying);
        progress.setVisibility(View.VISIBLE);
        retry.setVisibility(View.GONE);
        String token = session.token();
        executor.execute(() -> {
            try {
                int code = api.verify(token);
                runOnUiThread(() -> {
                    if (isDestroyed() || isFinishing()) return;
                    verifying = false;
                    progress.setVisibility(View.GONE);
                    if (!session.isValid() || code == 401) { leave(true); return; }
                    if (code == 200) {
                        status.setText(R.string.welcome_session);
                        identity.setText(getString(R.string.session_identity, session.email(), session.role()));
                    } else showError(R.string.server_error);
                });
            } catch (Exception exception) {
                int error = exception instanceof IOException ? R.string.connection_error : R.string.server_error;
                runOnUiThread(() -> {
                    if (isDestroyed() || isFinishing()) return;
                    verifying = false;
                    showError(error);
                });
            }
        });
    }

    private void showError(int error) {
        progress.setVisibility(View.GONE);
        status.setText(error);
        retry.setVisibility(View.VISIBLE);
    }

    private void leave(boolean expired) {
        if (isFinishing()) return;
        session.clear();
        handler.removeCallbacks(expire);
        startActivity(new Intent(this, LoginActivity.class).putExtra("expired", expired)
                .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK));
        finish();
    }

    @Override protected void onPause() {
        handler.removeCallbacks(expire);
        identity.setText("");
        super.onPause();
    }

    @Override protected void onDestroy() {
        handler.removeCallbacks(expire);
        executor.shutdownNow();
        api.cancel();
        super.onDestroy();
    }
}
