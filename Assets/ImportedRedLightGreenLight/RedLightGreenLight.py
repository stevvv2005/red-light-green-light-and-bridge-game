import cv2
import os
import numpy as np
import time
import threading
import mediapipe as mp
import pygame
import sys
import subprocess

# Folder paths
folderPath = 'frames'
soundPath = 'sounds'

# Load and sort frames
mylist = sorted(os.listdir(folderPath), key=lambda x: int(x.split('.')[0]))
graphic = [cv2.imread(f'{folderPath}/{imPath}') for imPath in mylist]

green, red, kill, winner, intro = graphic

# Init MediaPipe Pose
mp_pose = mp.solutions.pose
pose = mp_pose.Pose(model_complexity=0, min_detection_confidence=0.5, min_tracking_confidence=0.5)
mp_drawing = mp.solutions.drawing_utils

# Init pygame mixer
pygame.mixer.init()

def play_sound(path):
    def _play():
        pygame.mixer.music.load(path)
        pygame.mixer.music.play()
    threading.Thread(target=_play, daemon=True).start()

def restart_script():
    python = sys.executable
    subprocess.Popen([python] + sys.argv)
    sys.exit()

cv2.imshow('Squid Game', cv2.resize(intro, (0, 0), fx=0.5, fy=0.5))
cv2.waitKey(125)
play_sound(os.path.join(soundPath, 'squidWin.mp3'))
while True:
    cv2.imshow('Squid Game', cv2.resize(intro, (0, 0), fx=0.5, fy=0.5))
    if cv2.waitKey(10) & 0xFF == ord('q'):
        break

TIMER_MAX = 30
maxMove = 6500000
font = cv2.FONT_HERSHEY_SIMPLEX
cap = cv2.VideoCapture(0)

win = False
start_time = time.time()
light_timer = start_time
light_duration = 5
ref = None
isgreen = True
transition_frames = 10
player_name = "Player 101"
player_score = 0
current_frame = cv2.resize(green, (0, 0), fx=0.5, fy=0.5)

while cap.isOpened():
    ret, frame = cap.read()
    if not ret:
        break

    frameRGB = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    result = pose.process(frameRGB)

    if result.pose_landmarks:
        mp_drawing.draw_landmarks(frame, result.pose_landmarks, mp_pose.POSE_CONNECTIONS,
                                  landmark_drawing_spec=mp_drawing.DrawingSpec(color=(0, 255, 0), thickness=2, circle_radius=3),
                                  connection_drawing_spec=mp_drawing.DrawingSpec(color=(255, 255, 0), thickness=2))
        h, w, _ = frame.shape
        left_ear = result.pose_landmarks.landmark[mp_pose.PoseLandmark.LEFT_EAR]
        right_ear = result.pose_landmarks.landmark[mp_pose.PoseLandmark.RIGHT_EAR]

        head_x = int(((left_ear.x + right_ear.x) / 2) * w)
        head_y = int(((left_ear.y + right_ear.y) / 2) * h) - 40

        text = player_name
        text_scale = 1
        text_thickness = 3
        text_size, _ = cv2.getTextSize(text, font, text_scale, text_thickness)
        text_x = head_x - text_size[0] // 2
        text_y = head_y
        cv2.putText(frame, text, (text_x + 2, text_y + 2), font, text_scale, (0, 0, 0), text_thickness + 1)
        cv2.putText(frame, text, (text_x, text_y), font, text_scale, (203, 0, 255), text_thickness)

    elapsed = int(time.time() - start_time)
    remaining = max(0, TIMER_MAX - elapsed)
    display_frame = current_frame.copy()
    display_frame = cv2.putText(display_frame, str(remaining), (50, 50), font, 1,
                                (0, int(255 * remaining / TIMER_MAX), int(255 * (TIMER_MAX - remaining) / TIMER_MAX)), 4)

    if remaining == 0:
        win = True
        player_score = TIMER_MAX
        break

    if time.time() - light_timer >= light_duration:
        light_timer = time.time()
        isgreen = not isgreen
        target_frame = cv2.resize(green if isgreen else red, (0, 0), fx=0.5, fy=0.5)

        if isgreen:
            play_sound(os.path.join(soundPath, 'greenLight.mp3'))
        else:
            play_sound(os.path.join(soundPath, 'redLight.mp3'))

        for i in range(1, transition_frames + 1):
            alpha = i / transition_frames
            current_frame = cv2.addWeighted(current_frame, 1 - alpha, target_frame, alpha, 0)
            temp_display = current_frame.copy()
            temp_display = cv2.putText(temp_display, str(remaining), (50, 50), font, 1,
                                       (0, int(255 * remaining / TIMER_MAX), int(255 * (TIMER_MAX - remaining) / TIMER_MAX)), 4)
            camShow = cv2.resize(frame, (0, 0), fx=0.3, fy=0.3)
            camH, camW = camShow.shape[:2]
            temp_display[0:camH, -camW:] = camShow
            cv2.imshow('Squid Game', temp_display)
            if cv2.waitKey(1) & 0xFF == ord('q'):
                break
        current_frame = target_frame.copy()
        if not isgreen:
            ref = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)

    if not isgreen and ref is not None:
        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        frameDelta = cv2.absdiff(ref, gray)
        thresh = cv2.threshold(frameDelta, 20, 255, cv2.THRESH_BINARY)[1]
        change = np.sum(thresh)
        if change > maxMove:
            break

    if isgreen and (cv2.waitKey(1) & 0xFF == ord('w')):
        win = True
        player_score = remaining
        break

    camShow = cv2.resize(frame, (0, 0), fx=0.3, fy=0.3)
    camH, camW = camShow.shape[:2]
    display_frame[0:camH, -camW:] = camShow

    cv2.imshow('Squid Game', display_frame)
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()

if not win:
    final_frame = cv2.resize(kill, (0, 0), fx=0.5, fy=0.5)
    text = "GAME OVER"
    text_scale = 2
    text_thickness = 4
    text_size, _ = cv2.getTextSize(text, font, text_scale, text_thickness)
    x_center = (final_frame.shape[1] - text_size[0]) // 2
    y_center = final_frame.shape[0] // 2
    cv2.putText(final_frame, text, (x_center + 2, y_center + 2), font, text_scale, (0, 0, 0), text_thickness + 2)
    cv2.putText(final_frame, text, (x_center, y_center), font, text_scale, (0, 0, 255), text_thickness)

    play_sound(os.path.join(soundPath, 'kill.mp3'))
    while True:
        cv2.imshow('Squid Game', final_frame)
        key = cv2.waitKey(10) & 0xFF
        if key == ord('r'):
            restart_script()
        elif key == ord('q'):
            break
else:
    final_frame = cv2.resize(winner, (0, 0), fx=0.5, fy=0.5)
    win_text = f"{player_name} Wins!"
    score_text = f"Score: {player_score}"

    text_scale = 1.8
    score_scale = 1.5
    thickness = 3
    shadow = 2

    text_size, _ = cv2.getTextSize(win_text, font, text_scale, thickness)
    text_x = (final_frame.shape[1] - text_size[0]) // 2
    text_y = final_frame.shape[0] // 2

    score_size, _ = cv2.getTextSize(score_text, font, score_scale, thickness)
    score_x = (final_frame.shape[1] - score_size[0]) // 2
    score_y = text_y + 80

    cv2.putText(final_frame, win_text, (text_x + shadow, text_y + shadow), font, text_scale, (0, 0, 0), thickness + 2)
    cv2.putText(final_frame, score_text, (score_x + shadow, score_y + shadow), font, score_scale, (0, 0, 0), thickness + 2)
    cv2.putText(final_frame, win_text, (text_x, text_y), font, text_scale, (255, 20, 147), thickness)
    cv2.putText(final_frame, score_text, (score_x, score_y), font, score_scale, (0, 255, 255), thickness)

    play_sound(os.path.join(soundPath, 'win.mp3'))
    while True:
        cv2.imshow('Squid Game', final_frame)
        key = cv2.waitKey(10) & 0xFF
        if key == ord('r'):
            restart_script()
        elif key == ord('q'):
            break

cv2.destroyAllWindows()
